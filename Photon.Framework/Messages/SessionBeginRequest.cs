﻿using Photon.Communication;

namespace Photon.Framework.Messages
{
    public class SessionBeginRequest : IRequestMessage
    {
        public string MessageId {get; set;}
    }
}