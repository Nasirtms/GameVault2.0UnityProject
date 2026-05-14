using System;
using System.Collections.Generic;

public class WebSocketMessages
{
    [Serializable]
    public class Request_Base
    {
        public string requestId;
        public string type;
    }

    [Serializable]
    public class Response_Base
    {
        public string requestId;
        public string type;
        public bool success;
        public string error;
    }

    // -----------------------


    [Serializable]
    public class HeartbeatMessage_Sent : Request_Base
    {
        public HeartbeatMessage_Sent()
        {
            type = "pong";
        }
    }

    [Serializable]
    public class HeartbeatMessage_Received : Response_Base
    {

    }
}
