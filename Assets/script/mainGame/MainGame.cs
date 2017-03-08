﻿using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using Assets.script.common;
using LitJson;
using UnityEngine.UI;

public class MainGame : MonoBehaviour {
    private long oldTick;
    private static long tickDelta = (long)(0.5 * 10000000);// 单位是100毫微秒，即2s更新一次
    public GameObject menuPanelObj;

    // Use this for initialization
    void Start () {
        this.oldTick = DateTime.Now.Ticks;
        this.menuPanelObj = GameObject.Find("MenuPanel");
        // 隐藏menuPanelObj
        this.menuPanelObj.SetActive(false);
        // 生成双方牌组
        GameObject cardPrefab = (GameObject)Resources.Load("fab/CardPrefab");
        GameObject myCardObj = Instantiate(cardPrefab);
        GameObject enemyCardObj = Instantiate(cardPrefab);
        GameObject myDeckObj = GameObject.Find("MyPanel/FeatureDeck1/MainDeck");
        GameObject enemyDeckObj = GameObject.Find("EnemyPanel/FeatureDeck1/MainDeck");
        PutCard(myDeckObj, myCardObj);
        PutCard(enemyDeckObj, enemyCardObj);
    }

    // Update is called once per frame
    void Update()
    {
        // 定时获取服务器游戏数据
        if (DateTime.Now.Ticks > this.oldTick + MainGame.tickDelta)
        {
            GetServiceGameLog();
            this.oldTick = DateTime.Now.Ticks;
        }
    }

    void PutCard(GameObject contentObj, GameObject cardObj, int mode = 0)
    {
        cardObj.transform.SetParent(contentObj.transform);
        cardObj.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0.5f);
        cardObj.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.5f);
        cardObj.GetComponent<RectTransform>().localPosition = new Vector3(0, 0, 0);
        cardObj.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
            contentObj.GetComponent<RectTransform>().rect.width);
        cardObj.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
            contentObj.GetComponent<RectTransform>().rect.width / 230 * 160);
        if (mode == 1)
        {
            cardObj.GetComponent<RectTransform>().Rotate(new Vector3(0,0,90));
        }
    }
    
    public void GetServiceGameLog()
    {
        Dictionary<string, object> paramsMap = new Dictionary<string, object>();
        paramsMap.Add("token", UserInfo.token);
        string response = HttpClient.sendGet("http://localhost:8080/YgoService/duel-controller/get-inc-log",
            paramsMap);
        JsonData responseResult = JsonMapper.ToObject(response);
        if ((int)responseResult["code"] == 0)
        {
            // 非空表示有新消息
            if (responseResult["data"] != null)
            {
                Debug.Log(JsonMapper.ToJson(responseResult));
                string action = (string)responseResult["data"]["action"];
                switch (action)
                {
                    case "DrawCard":
                        DoDrawCard(responseResult);
                        break;
                    case "CallMonsterFromHand":
                        DoCallMonsterFromHand(responseResult);
                        break;
                }
            }
        }
        else
        {
            Debug.Log((string)responseResult["data"]);
        }
    }

    public void DoDrawCard(JsonData responseResult)
    {
        GameObject handContent;
        string imagePath;
        string cardId = (string)responseResult["data"]["paramsMap"]["CardId"];
        GameObject cardPrefab = (GameObject)Resources.Load("fab/CardPrefab");
        GameObject cardObject = Instantiate(cardPrefab);
        cardObject.name = "card" + cardId;
        cardObject.GetComponent<ShowCardInfo>().cardId = int.Parse(cardId);
        cardObject.GetComponent<ShowCardInfo>().cardInfoImageObj = GameObject.Find("InfoPanel/CardInfoPanel/CardImage");
        cardObject.GetComponent<ShowCardInfo>().cardInfoTextObj = GameObject.Find("InfoPanel/CardInfoPanel/Scroll View/Viewport/Content/Text");
        cardObject.AddComponent<CardMenuScript>();
        // 判断是敌方还是我方
        if ((string)responseResult["data"]["email"] == UserInfo.email)
        {
            handContent = GameObject.Find("MHandPanel/Scroll View/Viewport/Content");
            imagePath = "image/CardImage/" + cardId;
        }
        else
        {
            handContent = GameObject.Find("EHandPanel/Scroll View/Viewport/Content");
            imagePath = "image/CardBack";
        }
        cardObject.GetComponent<Image>().sprite = Resources.Load<Sprite>(imagePath);
        cardObject.transform.SetParent(handContent.transform);
    }

    public void DoCallMonsterFromHand(JsonData responseResult)
    {
        // 判断是敌方还是我方
        if ((string)responseResult["data"]["email"] == UserInfo.email)
        {
            int handCardIdx = int.Parse((string)responseResult["data"]["paramsMap"]["HandCardIdx"]);
            int monsterStatus = int.Parse((string)responseResult["data"]["paramsMap"]["MonsterStatus"]);
            int cardId = int.Parse((string)responseResult["data"]["paramsMap"]["CardId"]);
            int monsterCardIdx = int.Parse((string)responseResult["data"]["paramsMap"]["MonsterCardIdx"]);
            // 删除那张手牌
            GameObject MHandContentObj = GameObject.Find("MHandPanel/Scroll View/Viewport/Content");
            GameObject desHandCardObj = MHandContentObj.transform.GetChild(handCardIdx).gameObject;
            Destroy(desHandCardObj);
            // 场上生成怪兽
            GameObject cardPrefab = (GameObject)Resources.Load("fab/CardPrefab");
            GameObject cardObject = Instantiate(cardPrefab);
            cardObject.name = "card" + cardId.ToString();
            cardObject.GetComponent<ShowCardInfo>().cardId = cardId;
            cardObject.GetComponent<ShowCardInfo>().cardInfoImageObj = GameObject.Find("InfoPanel/CardInfoPanel/CardImage");
            cardObject.GetComponent<ShowCardInfo>().cardInfoTextObj = GameObject.Find("InfoPanel/CardInfoPanel/Scroll View/Viewport/Content/Text");
            cardObject.GetComponent<Image>().sprite = Resources.Load<Sprite>("image/CardImage/" + cardId.ToString());
            GameObject monsterContent = GameObject.Find("MyPanel/DuelDeck/Monster/Monster" + monsterCardIdx.ToString());
            PutCard(monsterContent, cardObject);
        }
        else
        {

        }
    }
}

