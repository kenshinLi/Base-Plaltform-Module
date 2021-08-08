using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlatformSystem.ServiceLib.Define
{
    public enum TransactionServiceCommandID
    {       
        // 發起開洗分
        INITIATE_CASH_IN_OUT = 105,
        // 開洗分
        CASH_IN_OUT = 106,

        // 遊戲點數異動
        UPDATE_GAME_POINT = 201,
    }
}
