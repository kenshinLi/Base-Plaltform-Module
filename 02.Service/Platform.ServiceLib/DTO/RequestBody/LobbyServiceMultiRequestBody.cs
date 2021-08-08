//-----------------------------------------------------------------------
// <copyright file="ExecuteRquestBody.cs" company="PlatformSystem">
//    Copyright (c) PlatformSystem. All rights reserved.
// </copyright>
// <author>JamesLee</author>
//-----------------------------------------------------------------------

using CommonLib.Model;
using System;
using System.Collections.Generic;

namespace PlatformSystem.ServiceLib.Model.RequestBody
{
    /// <summary>
    /// ExecuteRquestBody
    /// </summary>
    public class LobbyServiceMultiRequestBody
    {
        #region Property

        /// <summary>
        /// Gets or sets Token
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// Gets or sets List
        /// </summary>
        public List<BaseRequestBody> List { get; set; }

        #endregion Property
    }
}
