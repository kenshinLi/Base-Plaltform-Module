using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlatformSystem.ServiceLib.Define
{
    public enum GameEventServiceCommandID
    {
        // 通知玩家已經離開平台
        NOTIFY_PLAYER_HAS_LEAVED_PLATFORM = 1001,
        // 通知玩家有一個開分
        NOTIFY_PLAYER_HAS_A_CASH_IN = 1002
    }
}
