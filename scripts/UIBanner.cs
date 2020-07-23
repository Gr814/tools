// UIBanner.lua
// @Author : Grovech
// @Date   : 2020/7/14
// @Desc   : 循环滑动banner

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(ScrollRect))]
public class UIBanner : MonoBehaviour
{
    public GameObject temp;
    public int componetCount;
    public int xSpace;
    public int speed;
    [Tooltip("更新增量")]
    public int delta;

    Dictionary<float, int> info = new Dictionary<float, int>();
    private List<GameObject> m_view_list = new List<GameObject>();
    private Action<int, GameObject> m_update;
    private RectTransform m_content;
    private ScrollRect m_scorllRect;
    private float m_componentWidth;
    float last_pos = float.MinValue;
    int m_item_length;
    float period;
    float windowWidth;

    protected void Awake()
    {
        m_scorllRect = GetComponent<ScrollRect>();
        m_content = m_scorllRect.content;
        SetTemp();
        InitScrollRect();
        InitContent();
    }

    protected void Start()
    {
        InitItemView();
        OnContentPosChange();
    }

    void SetTemp()
    {
        Vector2 anchor = new Vector2(0, 0.5f);
        RectTransform rect = temp.transform as RectTransform;
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        m_componentWidth = (temp.transform as RectTransform).sizeDelta.x;
    }

    private void InitContent()
    {
        Vector2 anchor = new Vector2(0, 0.5f);
        m_content.anchorMin = anchor;
        m_content.anchorMax = anchor;
        m_content.pivot = anchor;
    }

    private void InitScrollRect()
    {
        m_scorllRect.vertical = false;
        m_scorllRect.horizontal = true;
        m_scorllRect.movementType = ScrollRect.MovementType.Elastic;
        m_scorllRect.onValueChanged.AddListener(x => OnContentPosChange());
    }

    public void SetParam(int length, Action<int, GameObject> update)
    {
        m_item_length = length;
        m_update = update;
        period = m_item_length * (xSpace + m_componentWidth);
        windowWidth = (transform as RectTransform).sizeDelta.x;
        m_scorllRect.horizontal = length > 1;
    }

    private void InitItemView()
    {
        List<GameObject> list = new List<GameObject>();
        for (int i = 0; i < componetCount; i++)
            list.Add(Instantiate(temp, m_content));
        m_view_list = list;
    }

    void OnContentPosChange()
    {
        if (m_item_length < 1) return;
        ChangeContent();
        UpdateView();
    }

    private void UpdateView()
    {
        if (Math.Abs(m_content.anchoredPosition.x - last_pos) > delta)
        {
            var info = GetVisibleData();
            int index = 0;
            foreach (KeyValuePair<float, int> keyPair in info)
            {
                if (m_view_list.Count > index)
                {
                    var go = m_view_list[index++];
                    if (go)
                    {
                        RectTransform rect = go.transform as RectTransform;
                        rect.anchoredPosition = new Vector2(keyPair.Key, rect.anchoredPosition.y);
                        if (m_update != null)
                            m_update.Invoke(keyPair.Value, go);
                    }
                }
            }
            last_pos = m_content.anchoredPosition.x;
            UpdateFocusOn();
        }
    }

    private void ChangeContent()
    {
        if (Mathf.Abs(m_content.anchoredPosition.x) < period)
        {
            m_content.sizeDelta = new Vector2(m_content.sizeDelta.x + period, m_content.sizeDelta.y);
            m_content.anchoredPosition -= new Vector2(period, 0);
        }

        if (Mathf.Abs(m_content.anchoredPosition.x + m_content.sizeDelta.x - windowWidth) < period)
        {
            m_content.sizeDelta = new Vector2(m_content.sizeDelta.x + period, m_content.sizeDelta.y);
        }
    }

    Dictionary<float, int> GetVisibleData()
    {
        info.Clear();
        float contentPosX = m_content.anchoredPosition.x;
        float rangeMin = contentPosX < 0 ? -contentPosX % period : period - contentPosX / period;
        float multiple = (int)(-contentPosX / period);
        float rangeMax = rangeMin + (transform as RectTransform).sizeDelta.x;
        Vector2 range = new Vector2(rangeMin, rangeMax);
        if (rangeMax < period)
        {
            for (int i = 0; i < m_item_length; i++)
            {
                if (DataIsInRange(i + 1, range))
                    info.Add(multiple * period + i * (m_componentWidth + xSpace), i);
            }
        }
        else
        {
            for (int i = 0; i < m_item_length; i++)
            {
                bool b = DataIsInRange(i + 1, new Vector2(0, rangeMax - period));
                float times = b ? multiple + 1 : multiple;

                if (b || DataIsInRange(i + 1, new Vector2(rangeMin, period)))
                    info.Add(times * period + i * (m_componentWidth + xSpace), i);
            }
        }
        return info;
    }

    bool DataIsInRange(int index, Vector2 range)
    {
        float max = (m_componentWidth + xSpace) * index;
        Vector2 dataRange = new Vector2(max - m_componentWidth - xSpace, max - xSpace);

        if (dataRange.y < range.x || dataRange.x > range.y)
            return false;
        return true;
    }

    
    // todo 分离动画
    void ResetAutoSlide()
    {
        if (gameObject.activeInHierarchy)
            StartCoroutine(AutoSlide());
    }

    IEnumerator AutoSlide()
    {
        yield return new WaitForEndOfFrame();
        float remaind = GetRemaind();
        while (true && m_item_length > 1)
        {
            float pre = GetRemaind();
            float delta = speed * Time.deltaTime;
            m_content.anchoredPosition -= new Vector2(delta, 0);
            remaind = GetRemaind();
            UpdateView();
            if (pre < remaind)
                break;
            yield return new WaitForEndOfFrame();
        }
        
        yield return new WaitForSeconds(2);
        ResetAutoSlide();
    }

    Action<int> m_focuson;

    public void SetFocusonCallback(Action<int> focuson)
    {
        m_focuson = focuson;
    }

    private void UpdateFocusOn()
    {
        var l = - m_content.anchoredPosition.x;
        var unit = (m_componentWidth + xSpace);
        var half_unit = unit / 2;
        var index = (int)(l / unit); 
        var remain = l % unit;
        index = index + (remain > half_unit ? 2 : 1); 
        index = (index - 1)% m_item_length;
        if (m_focuson != null)
        {
            m_focuson.Invoke(index);
        }
    }

    protected void OnEnable()
    {
        StartCoroutine(AutoSlide());
    }

    protected void OnDisable()
    {
        StopCoroutine("AutoSlide");
    }

    float GetRemaind()
    {
        float contentPosX = m_content.anchoredPosition.x;
        float rangeMin = -contentPosX % (m_componentWidth + xSpace);
        return m_componentWidth + xSpace - rangeMin;
    }
}
