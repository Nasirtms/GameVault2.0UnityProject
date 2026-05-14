// Assets/Scripts/Bet/BetManager.cs
using System;
using System.Collections.Generic;
using UnityEngine;

namespace HeadTailGame
{
    public class HeadTailBetManager : MonoBehaviour
    {
        [SerializeField] private HeadTailGameSettings config;
        

        public event Action<float> OnBetChanged;

        public float CurrentBet => config.betOptions[_index];
        public IReadOnlyList<float> Options => config.betOptions;

        int _index=0;
        void Awake()
        {
            if (config == null || config.betOptions == null || config.betOptions.Count == 0)
            {
                enabled = false; return;
            }

        }
        void Start() 
        {
            OnBetChanged?.Invoke(CurrentBet);
        }

        public void Increase()
        {
            _index = (_index + 1) % config.betOptions.Count;
            OnBetChanged?.Invoke(CurrentBet);
        }

        public void Decrease()
        {
            _index = (_index - 1 + config.betOptions.Count) % config.betOptions.Count;
            OnBetChanged?.Invoke(CurrentBet);
        }
    }
}