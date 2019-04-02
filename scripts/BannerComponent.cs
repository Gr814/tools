using ExoticBridge;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.UI.ScrollRect;

/// <summary>
/// 活动条
/// </summary>
[RequireComponent(typeof(ScrollRect))]
public class BannerComponent : MonoSwitch
{
    private RectTransform m_content;
    private ScrollRect m_scorllRect;
    private List<object> m_data;
    private float m_componentWidth;
    private List<MonoBehaviour> m_component;

    public GameObject temp;

    public int componetCount;

    public int xSpace;

    public int speed;

    float period
    {
        get
        {
            return m_data.Count  * (xSpace + m_componentWidth);
        }
    }

    float windowWidth
    {
        get
        {
            return (transform as RectTransform).sizeDelta.x;
        }
    }


    protected override void OnAwake()
    {
        m_scorllRect = GetComponent<ScrollRect>();
        m_content = m_scorllRect.content;
        SetTemp();
        InitScrollRect();
        InitContent();
    }

    protected override void OnStart()
    {
        SetData(new List<object> { "", "", "", "", "", "" });
        OnContentPosChange();
        //ResetAutoSlide();
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
        m_scorllRect.movementType = MovementType.Elastic;
        m_scorllRect.onValueChanged.AddListener(x => OnContentPosChange());
    }

    public void SetData(List<object> obj)
    {
        m_data = obj;
        InitUIComponent();
    }

    //void LateUpdate()
    //{

    //}

    private void InitUIComponent()
    {
        List<MonoBehaviour> list = new List<MonoBehaviour>();
        for (int i = 0; i < componetCount; i++)
            list.Add(Instantiate(temp, m_content).GetComponent<MonoBehaviour>());
        m_component = list;
    }

    void OnContentPosChange()
    {
        if (m_data == null || m_data.Count < 1) return;
        ChangeContent();
        Draw();
    }

    private void Draw()
    {
        Dictionary<float, int> info = GetVisibleData();
        int count = 0;
        foreach (KeyValuePair<float, int> keyPair in info) 
        {
            MonoBehaviour ga = m_component[count++];
            ga.gameObject.SetActive(true);
            (ga as IUIDraw).Draw(m_data[keyPair.Value]);
            RectTransform rect = ga.transform as RectTransform;
            rect.anchoredPosition = new Vector2(keyPair.Key, rect.anchoredPosition.y);
        }
        for (; count < m_component.Count; count++)
        {
            m_component[count].gameObject.SetActive(false);
        }
    }

    private void ChangeContent()
    {
        if (Mathf.Abs( m_content.anchoredPosition.x ) < period)
        {
            m_content.sizeDelta = new Vector2(m_content.sizeDelta.x + period, m_content.sizeDelta.y);
            m_content.anchoredPosition -= new Vector2(period, 0);
        }

        if (Mathf.Abs(m_content.anchoredPosition.x + m_content.sizeDelta.x - windowWidth) < period)
        {
            m_content.sizeDelta = new Vector2(m_content.sizeDelta.x + period, m_content.sizeDelta.y);
        }
    }

    List<GameObject> GetInvisibilityComponent(out List<GameObject> visible)
    {
        visible = new List<GameObject>();
        List<GameObject> invisible = new List<GameObject>();
        int length = m_component.Count;
        for (int i = 0; i < length; i++)
        {
            if (BComponentVisible(m_component[i].gameObject))
                visible.Add(m_component[i].gameObject);
            else
                invisible.Add(m_component[i].gameObject);
        }
        return invisible;
    }

    bool BComponentVisible(GameObject component)
    {
        RectTransform rect = component.transform as RectTransform;
        Vector2 pos = rect.anchoredPosition;
        if (Mathf.Abs(pos.x)> m_componentWidth)
            return false;
        return true;
    }

    Dictionary<float, int> GetVisibleData()
    {
        Dictionary<float, int> info = new Dictionary<float, int>();
        float contentPosX = m_content.anchoredPosition.x;
        float rangeMin = contentPosX < 0 ? -contentPosX % period : period - contentPosX / period;
        float multiple = (int)(-contentPosX / period);
        float rangeMax = rangeMin + (transform as RectTransform).sizeDelta.x;
        Vector2 range = new Vector2(rangeMin, rangeMax);
        if (rangeMax < period)
        {
            for (int i = 0; i < m_data.Count; i++)
            {
                if (DataIsInRange(i + 1, range))
                    info.Add(multiple * period + i * (m_componentWidth + xSpace), i);
            }
        }
        else
        {
            for (int i = 0; i < m_data.Count; i++)
            {
                bool b = DataIsInRange(i + 1, new Vector2(0, rangeMax - period));
                float times = b ? multiple + 1 : multiple;

                if (b || DataIsInRange(i + 1, new Vector2(rangeMin, period)))
                    info.Add(times * period + i * (m_componentWidth + xSpace), i);
            }
        }
        return info;
    }

    bool DataIsInRange(int index,Vector2 range)
    {
        float max = (m_componentWidth + xSpace) * index;
        Vector2 dataRange = new Vector2(max - m_componentWidth - xSpace, max - xSpace);

        if (dataRange.y < range.x || dataRange.x > range.y)
            return false;
        return true;
    }

    void ResetAutoSlide()
    {
        //if (gameObject.activeInHierarchy)
            //StartCoroutine(AutoSlide());
    }

    IEnumerator AutoSlide()
    {
        yield return new WaitForEndOfFrame();
        float remaind = GetRemaind();
        while (true)
        {
            float pre = GetRemaind();
            float delta = speed * Time.deltaTime;
            m_content.anchoredPosition -= new Vector2(delta, 0);
            remaind = GetRemaind();
            if (pre < remaind)
                break;
            yield return new WaitForEndOfFrame();
        }

        yield return new WaitForSeconds(2);
        ResetAutoSlide();
    }

    protected override void OnEnable()
    {
        //StartCoroutine(AutoSlide());
    }

    protected override void OnDisable()
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
