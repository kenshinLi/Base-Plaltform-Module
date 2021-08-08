using CommonLib.Model;
using CommonLib.Service;
using Platform.ServiceLib.Define;
using System;

namespace Platform.ServiceLib.Service
{
    public class BaseService<TCommandID, TRequestBody> : BaseCommandService<TCommandID, TRequestBody>
        where TCommandID : struct, IConvertible
        where TRequestBody : BaseRequestBody
    {
        #region Property

        internal BaseService()
        {
            base.ExceptionMessage = new
            {
                Message = MessageCode.UNEXPECTED_ERROR.ToString(),
                MessageCode = (int)MessageCode.UNEXPECTED_ERROR
            };

            base.JsonExceptionMessage = new
            {
                Message = MessageCode.ILLEGAL_INPUT.ToString(),
                MessageCode = (int)MessageCode.ILLEGAL_INPUT
            };

            base.NoneExistMessage = new
            {
                Message = MessageCode.UNKNOWN_FUNCTION.ToString(),
                MessageCode = (int)MessageCode.UNKNOWN_FUNCTION
            };
        }

        #endregion Property

        #region Method        

        #endregion
    }

    public class BaseService<TInfo, TCommandID, TRequestBody> : BaseInfoCommandService<TInfo, TCommandID, TRequestBody>
        where TInfo : class
        where TCommandID : struct, IConvertible
        where TRequestBody : BaseRequestBody
    {
        #region Property

        internal BaseService()
        {
            base.ExceptionMessage = new
            {
                Message = MessageCode.UNEXPECTED_ERROR.ToString(),
                MessageCode = (int)MessageCode.UNEXPECTED_ERROR
            };

            base.JsonExceptionMessage = new
            {
                Message = MessageCode.ILLEGAL_INPUT.ToString(),
                MessageCode = (int)MessageCode.ILLEGAL_INPUT
            };

            base.NoneExistMessage = new
            {
                Message = MessageCode.UNKNOWN_FUNCTION.ToString(),
                MessageCode = (int)MessageCode.UNKNOWN_FUNCTION
            };
        }

        #endregion Property

        #region Method        

        #endregion
    }
}
