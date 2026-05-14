using System;
using System.Collections.Generic;

public class FishWSNetworkMessages
{
    //public enum RequestTypes
    //{
    //    fire,
    //    hit,
    //    fish-hidden
    //}

    [Serializable]
    public class Request_Base
    {
        public string requestId;
        public string type;
        public string gameId;
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
    public class BulletFire_Request : Request_Base
    {
        public string bulletId;
        public string bulletCost;
        //public long timestamp;

        public BulletFire_Request()
        {
            type = "fire";
        }
    }

    [Serializable]
    public class BulletFire_Response : Response_Base
    {
        public string bulletId;
        public float bulletCost;
        public float newBalance;
    }

    [Serializable]
    public class FishHit_Request : Request_Base
    {
        public string bulletId;
        public string fishId;
        public bool killedByBot;
        public bool killedByBomb;
        public string bulletCost;
        public List<string> fishIdsKilledByBomb = new List<string>();

        public FishHit_Request()
        {
            type = "hit";
        }
    }

    [Serializable]
    public class FishHit_Response : Response_Base
    {
        public string fishId;
        public int hitCount;
        public bool killed;
        public bool killedByBot;
        public bool killedByBomb;
        public float winAmount;
        public string bulletId;
        public float bulletCost;
        public float newBalance;
    }

    [Serializable]
    public class FishDespawn_Request : Request_Base
    {
        public string fishId;
        //public long timestamp;

        public FishDespawn_Request()
        {
            type = "fish-despawn";
        }
    }

    [Serializable]
    public class FishDespawn_Response : Response_Base
    {
        public string bulletId;
        public float damage;
        public int seed;
    }
}