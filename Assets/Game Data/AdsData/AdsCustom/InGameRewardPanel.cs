using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InGameRewardPanel : MonoBehaviour
{
   [SerializeField] private GameObject _crateDropable;
   [SerializeField] private GameObject _player;
   [SerializeField] private Button _rewardButton;

   private void Start()
   {
      _rewardButton.onClick.AddListener(OnClaimReward);
   }

   private void OnEnable()
   {
      Time.timeScale = 0f;
   }

   void OnClaimReward()
   {
      
      if (GoogleAdManager.Instance!=null && GoogleAdManager.Instance._rewardedIntersitial.CanShoRewardedInterstitial())
      {
         GoogleAdManager.Instance.ShowRewardedInterstitial();
         RewardedInterstitial.ClearReawardedInterstitialEvents();
         RewardedInterstitial.OnRewardedAdCompleted += () =>
         {
            PlaceCrate();
         };
      }
      else
      {
         PlaceCrate();
         PlayInterstitialAd();
      }
      
      
     
   }

   void PlaceCrate()
   {
      gameObject.SetActive(false);
      Vector3 spawnPosition = _player.transform.position + _player.transform.forward * 5;
      spawnPosition.y = _player.transform.position.y;
      GameObject obj = Instantiate(_crateDropable, spawnPosition, Quaternion.identity);
      obj.gameObject.SetActive(true);
   }

   void PlayInterstitialAd()
   {
      if(GoogleAdManager.Instance)
         GoogleAdManager.Instance.ShowAdmobInterstitial();
   }

   private void OnDisable()
   {
      Time.timeScale = 1f;
   }
}
