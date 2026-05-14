using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ApiClasses
{
    [System.Serializable]
    public enum GameActionType
    {
        GameOpen,
        GameClose,
        GamePause,
        GameResume
    }

    public class Request_Base
    {
        public string requestId;

        public Request_Base()
        {
            requestId = Guid.NewGuid().ToString();
        }
    }

    public class Response_Base
    {
        public bool success;
        public bool error;
    }

    [System.Serializable]
    public class GameActionEvent_Request : Request_Base
    {
        public string gameId;
        public string action;
    }

    [System.Serializable]
    public class GameActionEvent_Response : Response_Base
    {

    }
}
