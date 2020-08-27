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

namespace Framework
{

    [RequireComponent(typeof(ScrollRect))]
    public class UIBanner : MonoBehaviour
    {
        public GameObject temp;
        public int componetCount;
        public int xSpace;
        public int speed;
        [Tooltip("更新增量")]
        public int delta;
        public float waitTime = 5;

        Dictionary<float, int> info = new Dictionary<float, int>();
        private List<GameObject> m_view_list = new List<GameObject>();
        private Action<int, GameObject> m_update;
        private RectTransform m_content;
        private ScrollRect m_scorllRect;
        private float m_componentWidth;
        private float m_space;
        float last_pos = float.MinValue;
        int m_item_length;
        float period;
        float windowWidth;
        bool m_enable;

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
            m_space = m_componentWidth + xSpace;
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
            m_scorllRect.movementType = ScrollRect.MovementType.Elastic;
            m_scorllRect.onValueChanged.AddListener(x => OnContentPosChange());
        }

        public void SetParam(int length, bool autoDisable, Action<int, GameObject> update)
        {
            m_item_length = length;
            m_update = update;
            period = m_item_length * m_space;
            windowWidth = (transform as RectTransform).sizeDelta.x;
            m_enable = autoDisable;
            if (m_item_length == 1)
            {
                m_scorllRect.horizontal = false;
            }
            if (m_enable)
            {
                StartCoroutine(AutoSlide());
            }
        }

        public void OnUpdate()
        {
            if (m_item_length > 1)
            {
                UpdateInfo();
            }
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
                UpdateInfo();
            }
        }

        private void UpdateInfo()
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
            float contentPosX = -m_content.anchoredPosition.x;
            float rangeMin = contentPosX - contentPosX % m_space;
            var right_x = contentPosX + (transform as RectTransform).sizeDelta.x;
            float rangeMax = right_x + m_space - right_x % m_space;
            Vector2 range = new Vector2(rangeMin, rangeMax);
            for(float i = rangeMin; i < rangeMax; i += m_space)
            {
                info.Add(i, GetCurrentPosData(i)); 
            }
            return info;
        }

        bool DataIsInRange(int index, Vector2 range)
        {
            float max = m_space * index;
            Vector2 dataRange = new Vector2(max - m_space, max - xSpace);
            if (dataRange.y < range.x || dataRange.x > range.y)
                return false;
            return true;
        }

        int GetCurrentPosData(float pos)
        {
            var real_pos = pos % period;
            return Mathf.FloorToInt(real_pos / m_space);
        }


        // todo 分离动画
        void ResetAutoSlide()
        {
            if (m_enable)
            {
                if (gameObject.activeInHierarchy)
                {
                    StartCoroutine(AutoSlide());
                }
            }
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

            yield return new WaitForSeconds(waitTime);
            ResetAutoSlide();
        }


        public void MoveLeft()
        {
            StopAllCoroutines();
            StartCoroutine(AutoLeft());
        }

        IEnumerator AutoLeft()
        {
            yield return new WaitForEndOfFrame();
            var remaind_dis = GetRemaind();
            float sum = 0;
            while (m_item_length > 1)
            {
                float delta = speed * 30 * Time.deltaTime;
                sum += delta;
                m_content.anchoredPosition -= new Vector2(delta, 0);
                var diff = remaind_dis - sum;
                if (diff < 0)
                {
                    m_content.anchoredPosition -= new Vector2(diff, 0);

                    UpdateView();
                    StopCoroutine("AutoLeft");
                    break;
                }
                UpdateView();
                yield return new WaitForEndOfFrame();
            }
        }


        public void MoveRight()
        {
            StopAllCoroutines();
            StartCoroutine(AutoRight());
        }

        IEnumerator AutoRight()
        {
            var delta = speed * 30 * Time.deltaTime;
            m_content.anchoredPosition += new Vector2(delta, 0);
            yield return new WaitForEndOfFrame();
            var remaind_dis = m_space - GetRemaind();
            float sum = 0;

            while (m_item_length > 1)
            {
                delta = speed * 30 * Time.deltaTime;
                sum += delta;
                m_content.anchoredPosition += new Vector2(delta, 0);
                var diff = remaind_dis - sum;
                if (diff < 0)
                {
                    m_content.anchoredPosition += new Vector2(diff, 0);
                    UpdateView();
                    StopCoroutine("AutoRight");
                    break;
                }
                UpdateView();
                yield return new WaitForEndOfFrame();
            }
        }

        Action<int> m_focuson;

        public void SetFocusonCallback(Action<int> focuson)
        {
            m_focuson = focuson;
        }

        private void UpdateFocusOn()
        {
            var l = -m_content.anchoredPosition.x;
            var unit = m_space;
            var half_unit = unit / 2;
            var index = (int)(l / unit);
            var remain = l % unit;
            index = index + (remain > half_unit ? 2 : 1);
            index = (index - 1) % m_item_length;
            if (m_focuson != null)
            {
                m_focuson.Invoke(index);
            }
        }

        protected void OnEnable()
        {
            if (m_enable)
            {
                StartCoroutine(AutoSlide());
            }
        }

        protected void OnDisable()
        {
            StopCoroutine(AutoSlide());

        }

        float GetRemaind()
        {
            float contentPosX = m_content.anchoredPosition.x;
            float rangeMin = -contentPosX % m_space;
            return m_space - rangeMin;
        }
    }
}
