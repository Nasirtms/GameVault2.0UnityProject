using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GunDatabase", menuName = "Scriptable/GunDatabase")]
public class GunDatabase : ScriptableObject
{
    [Serializable]
    public class GunLevel
    {
        public string gunName;
        public GameObject fireSystemPrefab;
        public GameObject bulletPrefab;
        public int numberOfBullets = 1;
    }

    public List<GunLevel> gunLevels;
}
