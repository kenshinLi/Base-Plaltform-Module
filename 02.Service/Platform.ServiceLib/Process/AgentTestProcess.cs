
using CommonLib.Define;
using CommonLib.Extension;
using CommonLib.Helper;
using CommonLib.Interface;
using CommonLib.Model;
using CommonLib.Service;
using PlatformSystem.DAOLib.Defines;
using PlatformSystem.DAOLib.DTO.Agent;
using PlatformSystem.DAOLib.Factory;
using PlatformSystem.DAOLib.Model;
using PlatformSystem.ServiceLib.Define;
using PlatformSystem.ServiceLib.Helper;
using PlatformSystem.ServiceLib.Model.Agent;
using PlatformSystem.ServiceLib.Model.RequestBody;
using PlatformSystem.ServiceLib.Model.Setting;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PlatformSystem.ServiceLib.Process
{
    public class AgentTestProcess
    {
        private int execTimeTotal = 0;
        private int execTimesCount = 0;

        /// <summary>
        /// singleton
        /// </summary>
        private static AgentTestProcess singleton;

        #region Property

        /// <summary>
        /// Gets Instance
        /// </summary>
        public static AgentTestProcess Instance
        {
            get
            {
                if (singleton == null)
                {
                    singleton = new AgentTestProcess();
                }

                return singleton;
            }
        }

        public bool IsEnable = false;

        private static ILogger logger = LogManager.GetCurrentClassLogger();

        private string filename = "Setting/ProcessSetting.json";
        private static AgentTestSetting setting;

        #endregion Property

        #region Method

        public void Start()
        {
            if (IsEnable)
                return;
       
            IsEnable = true;
 
            var processCount = AppSettingService.Instace.ProcessCount;
            if (processCount < 1)
                processCount = 1;

            setting = JsonSettingHelper.GetSetting<AgentTestSetting>(filename);

            logger.Debug(string.Format("Start AgentTestProcess ProcessCount:{0}", processCount));

            Parallel.For(0, processCount, new ParallelOptions { MaxDegreeOfParallelism = processCount }, idx =>
            {
                Task.Run(() =>
                {
                    try
                    {
                        logger.Debug(string.Format("Start Process Idx:{0}", idx));
                        Process(idx);
                    }
                    catch (Exception ex)
                    {
                        logger.Fatal(ex);
                    }
                });
            });
        }

        public void Stop()
        {
            logger.Info("Stop AgentTestProcess");
            IsEnable = false;
        }

        public void Process(int idx)
        {
            if (idx > 0)
                filename = string.Format("Setting/ProcessSetting_{0}.json", idx);

            var agent = AgentHelper.GetAgent(new GetAgentContent { AgentCode = setting.AgentCode });
            if (agent == null)
                throw new Exception(string.Format("NULL AGENT CODE: {0}", setting.AgentCode));

            if (setting.ProcessIntervalTime < 0)
                setting.ProcessIntervalTime = 60;

            if (Enum.TryParse(setting.GetWagersContent.QueryMode.ToString(), out GetWagersQueryMode queryMode) == false)
                throw new Exception(string.Format("Fail QueryMode: {0}", setting.GetWagersContent.QueryMode));

            while (IsEnable)
            {
                var beginTime = DateTime.UtcNow;

                var result = GetWagers(idx, setting.GetWagersContent, agent);
                
                if(queryMode == GetWagersQueryMode.BATCH && result.List.Count > 0)
                {
                    setting.ProcessCount = result.TotalRowsCount;
                    setting.GetWagersContent.AboveSerial = result.List.Last().Serial;
                }
                else if (queryMode == GetWagersQueryMode.TIME_INTERVAL)
                {
                    setting.ProcessCount = result.List.Count;
                    
                    if (result.TotalRowsCount > setting.GetWagersContent.PageNo * setting.GetWagersContent.RowsPerPage)
                    {
                        setting.GetWagersContent.PageNo++;
                    }
                    else
                    {
                        setting.GetWagersContent.PageNo = 1;

                        var now = DateTime.UtcNow;
                        if (setting.GetWagersContent.EndDateTime < now.AddMinutes(-setting.QueryIntervalMinute))
                        {
                            setting.GetWagersContent.StartDateTime = setting.GetWagersContent.EndDateTime;
                            setting.GetWagersContent.EndDateTime = ((DateTime)setting.GetWagersContent.StartDateTime).AddMinutes(setting.QueryIntervalMinute);                            
                        }
                    }
                }

                logger.Info(string.Format("File:{0}, Setting:{1}", filename, JsonConvert.SerializeObject(setting)));
                JsonSettingHelper.SetSetting(filename, setting);

                int execTime = (int)new TimeSpan(DateTime.UtcNow.Ticks - beginTime.Ticks).TotalMilliseconds;

                execTimeTotal += execTime;
                execTimesCount++;
                if (execTimesCount == AppSettingService.Instace.MaxAvgExecTimes)
                {
                    var avgExecTime = execTimeTotal / AppSettingService.Instace.MaxAvgExecTimes;
                    logger.Debug("Idx:{0} AvgCommandExecTime:{1}ms", idx, avgExecTime);
                    execTimeTotal = 0;
                    execTimesCount = 0;
                }

                if (setting.GetWagersContent.PageNo == 1)
                    Thread.Sleep(setting.ProcessIntervalTime * 1000);
                else
                    Thread.Sleep(1000);
            }
        }

        private GetWagersResult GetWagers(int idx, GetWagersContent content, Agent agent)
        {
            IResponseMessage rsp = null;

            if (setting.LocalMode)
            {
                var rst = new ExecuteInfoBody<Agent, GetWagersContent>
                {
                    Info = agent,
                    Content = content
                };
                rsp = GetWagers(rst);
            }
            else
            {
                var body = new AgentServiceRequestBody
                {
                    AgentCode = agent.AgentCode,
                    AgentKey = agent.AgentKey,
                    CommandID = (int)AgentServiceCommandID.GET_WAGERS,
                    Content = content
                };

                var cipherText = SecurityHelper.RSAEncrypt(agent.PublicKey, JsonConvert.SerializeObject(body));
                var rst = new
                {
                    agent.AgentCode,
                    CipherText = cipherText
                };

                var compressedText = WebAPIService<GamePlatformServiceType>.Instance.Excute<string>(GamePlatformServiceType.AGENT_SERVICE, rst);
                if(string.IsNullOrEmpty(compressedText))
                    return new GetWagersResult();

                // RSADecrypt
                var decryptString = SecurityHelper.RSADecrypt(agent.AgentPrivateKey, compressedText);
                rsp = JsonConvert.DeserializeObject<ResponseMessage>(decryptString);
            }

            if (rsp == null)
            {
                logger.Warn(string.Format("Idx :{0} Response NULL", idx));
            }
            else
            {
                if (rsp.MessageCode == (int)MessageCode.SUCCESS)
                {
                    if (content.IsZipped)
                    {
                        dynamic data = rsp.Content;
                        if (data != null && data.ZippedData != null)
                        {
                            string zippedData = data.ZippedData;
                            if (string.IsNullOrEmpty(zippedData) == false)
                            {
                                //logger.Debug(string.Format("Idx :{0} Begin Decompress", idx));
                                var zipBuffer = Convert.FromBase64String(data.ZippedData.ToString());

                                var decompress = GzipHelper.Decompress(zipBuffer);

                                return JsonConvert.DeserializeObject<GetWagersResult>(decompress);
                                //logger.Debug(string.Format("Idx :{0} End Decompress", idx));
                            }
                        }
                    }
                    else
                    {
                        try
                        {
                            return JsonConvert.DeserializeObject<GetWagersResult>(JsonConvert.SerializeObject(rsp.Content));
                        }
                        catch (Exception ex)
                        {
                            logger.Error(string.Format("Idx :{0} Response: {1}", idx, JsonConvert.SerializeObject(rsp)));
                            //throw ex;
                        }
                    }
                }
                else
                    logger.Warn(string.Format("Idx :{0} Response: {1}", idx, JsonConvert.SerializeObject(rsp)));
            }

            return new GetWagersResult();
        }

        private IResponseMessage GetWagers(ExecuteInfoBody<Agent, GetWagersContent> body)
        {
            MessageCode messageCode = MessageCode.UNEXPECTED_ERROR;

            object content = null;
            List<Wager> queryList = null;
            int totalNumber = 0;

            //check QueryMode
            if (Enum.TryParse(body.Content.QueryMode.ToString(), out GetWagersQueryMode queryMode) == false)
                messageCode = MessageCode.ILLEGAL_INPUT;
            else
            {
                if (queryMode == GetWagersQueryMode.BATCH)
                {
                    if (body.Content.Count <= 0 || body.Content.Count > 1000)
                    {
                        messageCode = MessageCode.ILLEGAL_INPUT;
                    }
                    else
                    {
                        // BATCH
                        var dto = new GetWagersByBatchDTO();
                        dto.Mapper(body.Content);
                        dto.Mapper(body.Info);

                        if (string.IsNullOrEmpty(body.Content.AboveSerial))
                            messageCode = DAOFactory.Agent.GetWagers(dto, out queryList);
                        else
                            messageCode = DAOFactory.Agent.GetWagersByBatch(dto, out queryList);

                        totalNumber = queryList.Count;
                    }
                }
                else if (queryMode == GetWagersQueryMode.TIME_INTERVAL)
                {
                    if (body.Content.StartDateTime == null ||
                        body.Content.EndDateTime == null ||
                        body.Content.RowsPerPage <= 0 ||
                        body.Content.RowsPerPage > 1000 ||
                        body.Content.PageNo <= 0)
                    {
                        messageCode = MessageCode.ILLEGAL_INPUT;
                    }
                    else
                    {
                        // TIME
                        var dto = new GetWagersByTimeDTO();
                        dto.Mapper(body.Content);
                        dto.Mapper(body.Info);

                        messageCode = DAOFactory.Agent.GetWagersByTime(dto, out queryList, out totalNumber);
                    }
                }
            }

            if (messageCode == MessageCode.SUCCESS)
            {
                var list = new List<GetWagersResultContent>();
                foreach (var item in queryList)
                    list.Add(new GetWagersResultContent().Mapper(item));

                content = new GetWagersResult
                {
                    TotalRowsCount = totalNumber,
                    List = list
                };

                if (body.Content.IsZipped)
                {
                    var zipBuffer = GzipHelper.Compress(JsonConvert.SerializeObject(content));
                    var compressedText = Convert.ToBase64String(zipBuffer);

                    content = new
                    {
                        ZippedData = compressedText
                    };
                }
            }
            else
                logger.Info("reqGuid:{0} GetWagers [ILLEGAL_INPUT]", body.ReqGUID);

            return new ResponseMessage
            {
                MessageCode = (int)messageCode,
                Content = content,
                Message = messageCode.ToString()
            };
        }

        #endregion
    }
}
