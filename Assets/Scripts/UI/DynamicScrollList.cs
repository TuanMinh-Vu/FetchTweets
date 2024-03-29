﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DynamicScrollList : MonoBehaviour
{
    

    #region Pool
    [Header("Pool")]
    [SerializeField] 
    private bool usingPool;

    [SerializeField] 
    private int poolAmount;

    private Stack<GameObject> pool = new Stack<GameObject>();


    #endregion

    #region List
    
    [System.Serializable]
    public enum Direction
    {
        Horizontal,
        Vertical
    }
    
    [Header("List")]
    
    [SerializeField] GameObject ItemHolder;
    [SerializeField] GameObject Item;
    [SerializeField] Vector2 ItemSpacing;

    public int numberOfItems;
    
    [SerializeField] Direction direction = Direction.Horizontal;

    [SerializeField] bool refreshAfterEnable = false;

    [SerializeField]
    private bool spawnedAtStart = true;
    
    #endregion
    
    #region Dots

    [Header("Dots")]
    [SerializeField]
    private Transform dotsContainer;

    [SerializeField]
    private GameObject dotObj;

    private List<RectTransform> dots = new List<RectTransform>();

    [SerializeField] 
    private int dotPoolAmount = 50;

    [SerializeField]
    private Color dotDisableColor;

    [SerializeField]
    private Color dotEnableColor;

    #endregion

    
    private Canvas mCanvas;
    private Rect panelDimensions;
    private Rect iconDimensions;
    private int amountPerPage;
    private int itemsCounter = 0;

    private TwitterManager twitterManager;
    private List<GameObject> tweets = new List<GameObject>();
    
    private void Start()
    {
        twitterManager = FindObjectOfType<TwitterManager>(); // TODO: Separate twitterManager from this script
        
        if(!refreshAfterEnable && spawnedAtStart)
        {
            mCanvas = FindObjectOfType<Canvas>();
            SetUpPanel();
        }
    }

    private void OnEnable() 
    {
        if(refreshAfterEnable && spawnedAtStart)
        {
            mCanvas = FindObjectOfType<Canvas>();
            SetUpPanel();
        }
    }

    public void Spawn()
    {
        mCanvas = FindObjectOfType<Canvas>();
        SetUpPanel();
    }

    private void SetUpPanel()
    {
        panelDimensions = ItemHolder.GetComponent<RectTransform>().rect;
        iconDimensions = Item.GetComponent<RectTransform>().rect;
        int maxInARow = Mathf.FloorToInt((panelDimensions.width + ItemSpacing.x) / (iconDimensions.width + ItemSpacing.x));
        int maxInACol = Mathf.FloorToInt((panelDimensions.height + ItemSpacing.y) / (iconDimensions.height + ItemSpacing.y));
        amountPerPage = maxInARow * maxInACol;
        int totalPages = Mathf.CeilToInt((float)numberOfItems / amountPerPage);
        
        // create dots
        if(totalPages > 1) // only create dots if have more than 1 page
        {
            if (usingPool)
            {
                for (int i = 0; i < dotPoolAmount; i++)
                {
                    GameObject newDot = Instantiate(dotObj, dotsContainer);
                    dots.Add(newDot.GetComponent<RectTransform>());
                    if(i >= totalPages) newDot.SetActive(false);
                }
            }
            else
            {
                for (int i = 0; i < totalPages; i++)
                {
                    GameObject newDot = Instantiate(dotObj, dotsContainer);
                    dots.Add(newDot.GetComponent<RectTransform>());
                }
            }
        }
        
        LoadPanels(totalPages);

        if(totalPages > 1) UpdateDotsColor(1); // highlight dot of page 1
    }

    private void UpdateDotsColor(int _currentPage)
    {
        for (int i = 0; i < dots.Count; i++)
        {
            if(!dots[i].gameObject.activeInHierarchy) return;
            int dotIndex = i + 1 > dots.Count ? 1 : i + 1;
            
            dots[i].GetComponent<Image>().color = dotIndex == _currentPage ? dotEnableColor: dotDisableColor;
        }
    }

    private void LoadPanels(int numberOfPanels)
    {
        GameObject panelClone = Instantiate(ItemHolder);
        
        PageSwiper swiper = ItemHolder.AddComponent<PageSwiper>();
        swiper.totalPages = numberOfPanels;
        swiper.horizontal = direction == Direction.Horizontal;
        
        swiper.OnSwiped += ()=>
        {
            // reposition page for pool
            if (usingPool)
            {
                var newPage = pool.Pop();
                newPage.GetComponent<RectTransform>().localPosition = direction == Direction.Horizontal ? new Vector2(mCanvas.GetComponent<RectTransform>().rect.width * (swiper.currentPage - 1), 0)
                    : new Vector2(0, - mCanvas.GetComponent<RectTransform>().rect.height * (swiper.currentPage - 1));
                pool.Push(newPage);
                
                var _publicName = twitterManager.results.statuses[swiper.currentPage - 1].user.screen_name;
                var _id = twitterManager.results.statuses[swiper.currentPage - 1].user.name;
                var _tweet = twitterManager.results.statuses[swiper.currentPage - 1].text;
                var _url = twitterManager.results.statuses[swiper.currentPage - 1].user.profile_image_url;
                newPage.GetComponentInChildren<Tweet>().LoadTweet(_url, _publicName, _id, _tweet);
            }
            
            UpdateDotsColor(swiper.currentPage);
        };

        int toBeSpawnedAmount = usingPool ? poolAmount : numberOfPanels;

        for (int i = 1; i <= toBeSpawnedAmount; i++)
        {
            GameObject panel = Instantiate(panelClone, mCanvas.transform, false);
            panel.transform.SetParent(ItemHolder.transform);
            panel.name = "Page-" + i;
            panel.AddComponent<Page>();
            
            if (usingPool)
            {
                pool.Push(panel);
            }

            panel.GetComponent<RectTransform>().localPosition = direction == Direction.Horizontal ? new Vector2(mCanvas.GetComponent<RectTransform>().rect.width * (i - 1), 0)
            : new Vector2(0, - mCanvas.GetComponent<RectTransform>().rect.height * (i - 1));

            SetUpGrid(panel);
            int numberOfIcons = i == numberOfPanels ? numberOfItems - itemsCounter : amountPerPage;
            
            if(!usingPool)
               LoadIcons(numberOfIcons, panel);
            else
                LoadIcons(1, panel); // TODO: create another pool for icons
        }
        Destroy(panelClone);
    }

    private void SetUpGrid(GameObject panel)
    {
        GridLayoutGroup grid = panel.GetComponent<GridLayoutGroup>();

        if(grid == null) grid = panel.AddComponent<GridLayoutGroup>();
        
        grid.cellSize = new Vector2(iconDimensions.width, iconDimensions.height);
        grid.childAlignment = TextAnchor.MiddleCenter;
        grid.spacing = ItemSpacing;
    }

    private void LoadIcons(int numberOfIcons, GameObject parentObject)
    {
        for (int i = 1; i <= numberOfIcons; i++)
        {
            itemsCounter++;
            GameObject icon = Instantiate(Item, mCanvas.transform, false);
            icon.transform.SetParent(parentObject.transform);
            tweets.Add(icon);
        }
    }
    
    private void OnDisable() 
    {
        if(refreshAfterEnable)
        {
            for (int i = 0; i < ItemHolder.transform.childCount; i++)
            {
                Destroy(ItemHolder.transform.GetChild(i).gameObject);
            }

            itemsCounter = 0;
            Destroy(ItemHolder.GetComponent<PageSwiper>());

            for (int i = 0; i < dotsContainer.childCount; i++)
            {
                Destroy(dotsContainer.GetChild(i).gameObject);
            }

            dots.Clear();
        }
    }

    public void OnFinishedSearching(int numOfResults)
    {
        if(numOfResults == 0) return;
        
        if (numberOfItems == 0)
        {
            numberOfItems =  numOfResults;
            Spawn();
        }
        else
        {
            var swiper = GetComponentInChildren<PageSwiper>();
            swiper.totalPages = numOfResults;
            swiper.ResetSwiper();

            foreach (var page in pool)
            {
                page.GetComponent<Page>().ResetPosition();
            }

            if (numOfResults > numberOfItems)
            {
                if (numOfResults <= dots.Count)
                {
                    // if the number of results is less than the amount of dots that were already spawned in the pool
                    for (int i = numberOfItems; i < numOfResults; i++)
                    {
                        dots[i].gameObject.SetActive(true);
                    }
                }
                else
                {
                    // if the amount of results is more than pool's size, enable all items in the pool first, then instantiate more 
                    for (int i = numberOfItems; i < dots.Count; i++)
                    {
                        dots[i].gameObject.SetActive(true);
                    }

                    int currentAmount = dots.Count;
                    for (int i = 0; i < numOfResults - currentAmount; i++)
                    {
                        GameObject newDot = Instantiate(dotObj, dotsContainer);
                        dots.Add(newDot.GetComponent<RectTransform>());
                    }
                }
            }
            else
            {
                for (int i = 1; i <= numberOfItems - numOfResults; i++)
                {
                    dots[numberOfItems - i].gameObject.SetActive(false);
                }
            }

            numberOfItems = numOfResults;
            UpdateDotsColor(1);
        }
        
        if(tweets.Count == 0) return;
        
        var _publicName = twitterManager.results.statuses[0].user.screen_name;
        var _id = twitterManager.results.statuses[0].user.name;
        var _tweet = twitterManager.results.statuses[0].text;
        var _url = twitterManager.results.statuses[0].user.profile_image_url;
        tweets[0].GetComponentInChildren<Tweet>()?.LoadTweet(_url, _publicName, _id, _tweet);
    }
}
