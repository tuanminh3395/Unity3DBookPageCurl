using System;
using System.Collections.Generic;
using FrogunnerGames.Coroutine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace FrogunnerGames.UI.Book
{
    public enum FlipMode
    {
        RightToLeft,
        LeftToRight
    }

//    [ExecuteInEditMode]
    public class Book : MonoBehaviour
    {
        [SerializeField] private Canvas _canvas;
        [SerializeField] private RectTransform _bookTransform;
        [SerializeField] private RectTransform _clippingPanelTransform;
        [SerializeField] private RectTransform _shadowLTRTransform;
        [SerializeField] private RectTransform _nextPageClipTransform;
        [SerializeField] private RectTransform _shadowTransform;
        [SerializeField] private RectTransform _rightTransform;
        [SerializeField] private RectTransform _leftTransform;
        [SerializeField] private RectTransform _rightNextTransform;
        [SerializeField] private RectTransform _leftNextTransform;
        [SerializeField] private EventTrigger _rightHotspot;
        [SerializeField] private EventTrigger _leftHotspot;
        [SerializeField] private int _startingPage;
        [SerializeField] private bool _interactable = true;
        [SerializeField] private bool _enableShadowEffect = true;
        [SerializeField] private RectTransform[] _bookPages;

        public int StartingPage => _startingPage;

        //represent the index of the sprite shown in the right page
        public int CurrentPage { private set; get; }

        public int TotalPageCount
        {
            get { return _bookPages.Length; }
        }

        public Vector3 EndBottomLeft
        {
            get { return _edgeBottomLeft; }
        }

        public Vector3 EndBottomRight
        {
            get { return _edgeBottomRight; }
        }

        public float Height
        {
            get { return _bookTransform.rect.height; }
        }

        public UnityEvent OnFlip;

        private float radius1, radius2;

        //Spine Bottom
        private Vector3 _spineBottom;

        //Spine Top
        private Vector3 _spineTop;

        //corner of the page
        private Vector3 _pageCorner;

        //Edge Bottom Right
        private Vector3 _edgeBottomRight;

        //Edge Bottom Left
        private Vector3 _edgeBottomLeft;

        //follow point 
        private Vector3 _followPoint;

        private bool pageDragging;

        //current flip mode
        private FlipMode mode;

        private RectTransform _currentLeftPage;
        private RectTransform _currentRightPage;

        private void Start()
        {
            float scaleFactor = 1;
            if (_canvas) scaleFactor = _canvas.scaleFactor;

            var rect = _bookTransform.rect;
            float pageWidth = (rect.width * scaleFactor - 1) / 2;
            float pageHeight = rect.height * scaleFactor;

            _leftTransform.gameObject.SetActive(false);
            _rightTransform.gameObject.SetActive(false);

            var bookPosition = _bookTransform.position;
            Vector3 globalsb = bookPosition + new Vector3(0, -pageHeight / 2);
            _spineBottom = transformPoint(globalsb);
            Vector3 globalebr = bookPosition + new Vector3(pageWidth, -pageHeight / 2);
            _edgeBottomRight = transformPoint(globalebr);
            Vector3 globalebl = bookPosition + new Vector3(-pageWidth, -pageHeight / 2);
            _edgeBottomLeft = transformPoint(globalebl);
            Vector3 globalst = bookPosition + new Vector3(0, pageHeight / 2);

            _spineTop = transformPoint(globalst);
            radius1 = Vector2.Distance(_spineBottom, _edgeBottomRight);

            float scaledPageWidth = pageWidth / scaleFactor;
            float scaledPageHeight = pageHeight / scaleFactor;
            radius2 = Mathf.Sqrt(scaledPageWidth * scaledPageWidth + scaledPageHeight * scaledPageHeight);
            _clippingPanelTransform.sizeDelta = new Vector2(scaledPageWidth * 2, scaledPageHeight + scaledPageWidth * 2);
            _shadowTransform.sizeDelta = new Vector2(scaledPageWidth, scaledPageHeight + scaledPageWidth * 0.6f);
            _shadowLTRTransform.sizeDelta = new Vector2(scaledPageWidth, scaledPageHeight + scaledPageWidth * 0.6f);
            _nextPageClipTransform.sizeDelta = new Vector2(scaledPageWidth, scaledPageHeight + scaledPageWidth * 0.6f);

            _rightHotspot.AddEntry(EventTriggerType.BeginDrag, OnMouseDragRightPage);
            _rightHotspot.AddEntry(EventTriggerType.EndDrag, OnMouseRelease);
            _rightHotspot.AddEntry(EventTriggerType.Drag, OnDrag);

            _leftHotspot.AddEntry(EventTriggerType.BeginDrag, OnMouseDragLeftPage);
            _leftHotspot.AddEntry(EventTriggerType.EndDrag, OnMouseRelease);
            _leftHotspot.AddEntry(EventTriggerType.Drag, OnDrag);

            // Update starting pages
            CurrentPage = _startingPage;
            UpdatePages();
        }

        private Vector3 transformPoint(Vector3 global)
        {
            Vector2 localPos = _bookTransform.InverseTransformPoint(global);
            //RectTransformUtility.ScreenPointToLocalPointInRectangle(BookPanel, global, null, out localPos);
            return localPos;
        }

        private void OnDrag()
        {
            if (pageDragging && _interactable)
            {
                UpdateBook();
            }
        }

        private void UpdateBook()
        {
            _followPoint = Vector3.Lerp(_followPoint, transformPoint(Input.mousePosition), Time.deltaTime * 10);
            if (mode == FlipMode.RightToLeft)
                UpdateBookRTLToPoint(_followPoint);
            else
                UpdateBookLTRToPoint(_followPoint);
        }

        public void UpdateBookLTRToPoint(Vector3 followLocation)
        {
            mode = FlipMode.LeftToRight;
            _followPoint = followLocation;
            _shadowLTRTransform.SetParent(_clippingPanelTransform, true);
            _shadowLTRTransform.localPosition = new Vector3(0, 0, 0);
            _shadowLTRTransform.localEulerAngles = new Vector3(0, 0, 0);
            _leftTransform.SetParent(_clippingPanelTransform, true);

            _rightTransform.SetParent(_bookTransform, true);
            _leftNextTransform.SetParent(_bookTransform, true);

            _pageCorner = Calc_C_Position(followLocation);
            float T0_T1_Angle = Calc_T0_T1_Angle(_pageCorner, _edgeBottomLeft, out var t1);
            if (T0_T1_Angle < 0) T0_T1_Angle += 180;

            _clippingPanelTransform.eulerAngles = new Vector3(0, 0, T0_T1_Angle - 90);
            _clippingPanelTransform.position = _bookTransform.TransformPoint(t1);

            //page position and angle
            _leftTransform.position = _bookTransform.TransformPoint(_pageCorner);
            float C_T1_dy = t1.y - _pageCorner.y;
            float C_T1_dx = t1.x - _pageCorner.x;
            float C_T1_Angle = Mathf.Atan2(C_T1_dy, C_T1_dx) * Mathf.Rad2Deg;
            _leftTransform.eulerAngles = new Vector3(0, 0, C_T1_Angle - 180);

            _nextPageClipTransform.eulerAngles = new Vector3(0, 0, T0_T1_Angle - 90);
            _nextPageClipTransform.position = _bookTransform.TransformPoint(t1);
            _leftNextTransform.SetParent(_nextPageClipTransform, true);
            _rightTransform.SetParent(_clippingPanelTransform, true);
            _rightTransform.SetAsFirstSibling();

            _shadowLTRTransform.SetParent(_leftTransform, true);
        }

        public void UpdateBookRTLToPoint(Vector3 followLocation)
        {
            mode = FlipMode.RightToLeft;
            _followPoint = followLocation;
            _shadowTransform.SetParent(_clippingPanelTransform, true);
            _shadowTransform.localPosition = new Vector3(0, 0, 0);
            _shadowTransform.localEulerAngles = new Vector3(0, 0, 0);
            _rightTransform.SetParent(_clippingPanelTransform, true);

            _leftTransform.SetParent(_bookTransform, true);
            _rightNextTransform.SetParent(_bookTransform, true);
            _pageCorner = Calc_C_Position(followLocation);
            Vector3 t1;
            float T0_T1_Angle = Calc_T0_T1_Angle(_pageCorner, _edgeBottomRight, out t1);
            if (T0_T1_Angle >= -90) T0_T1_Angle -= 180;

            _clippingPanelTransform.pivot = new Vector2(1, 0.35f);
            _clippingPanelTransform.eulerAngles = new Vector3(0, 0, T0_T1_Angle + 90);
            _clippingPanelTransform.position = _bookTransform.TransformPoint(t1);

            //page position and angle
            _rightTransform.position = _bookTransform.TransformPoint(_pageCorner);
            float C_T1_dy = t1.y - _pageCorner.y;
            float C_T1_dx = t1.x - _pageCorner.x;
            float C_T1_Angle = Mathf.Atan2(C_T1_dy, C_T1_dx) * Mathf.Rad2Deg;
            _rightTransform.eulerAngles = new Vector3(0, 0, C_T1_Angle);

            _nextPageClipTransform.eulerAngles = new Vector3(0, 0, T0_T1_Angle + 90);
            _nextPageClipTransform.position = _bookTransform.TransformPoint(t1);
            _rightNextTransform.SetParent(_nextPageClipTransform, true);
            _leftTransform.SetParent(_clippingPanelTransform, true);
            _leftTransform.SetAsFirstSibling();

            _shadowTransform.SetParent(_rightTransform, true);
        }

        private float Calc_T0_T1_Angle(Vector3 c, Vector3 bookCorner, out Vector3 t1)
        {
            Vector3 t0 = (c + bookCorner) / 2;
            float T0_CORNER_dy = bookCorner.y - t0.y;
            float T0_CORNER_dx = bookCorner.x - t0.x;
            float T0_CORNER_Angle = Mathf.Atan2(T0_CORNER_dy, T0_CORNER_dx);

            float T1_X = t0.x - T0_CORNER_dy * Mathf.Tan(T0_CORNER_Angle);
            T1_X = normalizeT1X(T1_X, bookCorner, _spineBottom);
            t1 = new Vector3(T1_X, _spineBottom.y, 0);

            //clipping plane angle=T0_T1_Angle
            float T0_T1_dy = t1.y - t0.y;
            float T0_T1_dx = t1.x - t0.x;
            float T0_T1_Angle = Mathf.Atan2(T0_T1_dy, T0_T1_dx) * Mathf.Rad2Deg;
            return T0_T1_Angle;
        }

        private float normalizeT1X(float t1, Vector3 corner, Vector3 sb)
        {
            if (t1 > sb.x && sb.x > corner.x)
                return sb.x;
            if (t1 < sb.x && sb.x < corner.x)
                return sb.x;
            return t1;
        }

        private Vector3 Calc_C_Position(Vector3 followLocation)
        {
            Vector3 c;
            _followPoint = followLocation;
            float F_SB_dy = _followPoint.y - _spineBottom.y;
            float F_SB_dx = _followPoint.x - _spineBottom.x;
            float F_SB_Angle = Mathf.Atan2(F_SB_dy, F_SB_dx);
            Vector3 r1 = new Vector3(radius1 * Mathf.Cos(F_SB_Angle), radius1 * Mathf.Sin(F_SB_Angle), 0) + _spineBottom;

            float F_SB_distance = Vector2.Distance(_followPoint, _spineBottom);
            if (F_SB_distance < radius1)
                c = _followPoint;
            else
                c = r1;
            float F_ST_dy = c.y - _spineTop.y;
            float F_ST_dx = c.x - _spineTop.x;
            float F_ST_Angle = Mathf.Atan2(F_ST_dy, F_ST_dx);
            Vector3 r2 = new Vector3(radius2 * Mathf.Cos(F_ST_Angle),
                             radius2 * Mathf.Sin(F_ST_Angle), 0) + _spineTop;
            float C_ST_distance = Vector2.Distance(c, _spineTop);
            if (C_ST_distance > radius2)
                c = r2;
            return c;
        }

        public void DragRightPageToPoint(Vector3 point)
        {
            if (CurrentPage >= _bookPages.Length) return;
            pageDragging = true;
            mode = FlipMode.RightToLeft;
            _followPoint = point;


            _nextPageClipTransform.pivot = new Vector2(0, 0.12f);
            _clippingPanelTransform.pivot = new Vector2(1, 0.35f);

            _leftTransform.gameObject.SetActive(true);
            _leftTransform.pivot = new Vector2(0, 0);
            var rightNextPosition = _rightNextTransform.position;
            _leftTransform.position = rightNextPosition;
            _leftTransform.eulerAngles = new Vector3(0, 0, 0);

            var leftPage = _bookPages[CurrentPage + 1];
            SetPage(leftPage, _leftTransform);
            _leftTransform.SetAsFirstSibling();

            _rightTransform.gameObject.SetActive(true);
            _rightTransform.position = rightNextPosition;
            _rightTransform.eulerAngles = new Vector3(0, 0, 0);
            var rightPage = _bookPages[CurrentPage + 2];
            SetPage(rightPage, _rightTransform);

            var rightNextPage = _bookPages[CurrentPage + 3];
            SetPage(rightNextPage, _rightNextTransform);

            _leftNextTransform.SetAsFirstSibling();
            if (_enableShadowEffect) _shadowTransform.gameObject.SetActive(true);
            UpdateBookRTLToPoint(_followPoint);
        }

        private void SetPage(RectTransform page, Transform parent)
        {
            page.SetParent(parent);
            page.localPosition = Vector3.zero;
            page.localScale = Vector3.one;
            page.localRotation = Quaternion.identity;
            page.anchoredPosition = new Vector2(0.5f, 0.5f);
            page.anchorMin = new Vector2(0f, 0f);
            page.anchorMax = new Vector2(1f, 1f);
            page.SetAsLastSibling();
            page.gameObject.SetActive(true);
        }

        private void OnMouseDragRightPage()
        {
            if (_interactable)
                DragRightPageToPoint(transformPoint(Input.mousePosition));
        }

        public void DragLeftPageToPoint(Vector3 point)
        {
            if (CurrentPage <= 0) return;
            pageDragging = true;
            mode = FlipMode.LeftToRight;
            _followPoint = point;

            _nextPageClipTransform.pivot = new Vector2(1, 0.12f);
            _clippingPanelTransform.pivot = new Vector2(0, 0.35f);

            _rightTransform.gameObject.SetActive(true);
            var leftNextPosition = _leftNextTransform.position;
            _rightTransform.position = leftNextPosition;

            var rightPage = _bookPages[CurrentPage];
            SetPage(rightPage, _rightTransform);

            _rightTransform.eulerAngles = Vector3.zero;
            _rightTransform.SetAsFirstSibling();

            _leftTransform.gameObject.SetActive(true);
            _leftTransform.pivot = new Vector2(1, 0);
            _leftTransform.position = leftNextPosition;
            _leftTransform.eulerAngles = Vector3.zero;

            var leftPage = _bookPages[CurrentPage - 1];
            SetPage(leftPage, _leftTransform);

            var leftNextPage = _bookPages[CurrentPage - 2];
            SetPage(leftNextPage, _leftNextTransform);

            _rightNextTransform.SetAsFirstSibling();
            if (_enableShadowEffect) _shadowLTRTransform.gameObject.SetActive(true);
            UpdateBookLTRToPoint(_followPoint);
        }

        private void OnMouseDragLeftPage()
        {
            if (_interactable)
                DragLeftPageToPoint(transformPoint(Input.mousePosition));
        }

        private void OnMouseRelease()
        {
            if (_interactable)
                ReleasePage();
        }

        public void ReleasePage()
        {
            if (!pageDragging) return;

            pageDragging = false;
            float distanceToLeft = Vector2.Distance(_pageCorner, _edgeBottomLeft);
            float distanceToRight = Vector2.Distance(_pageCorner, _edgeBottomRight);
            if (distanceToRight < distanceToLeft && mode == FlipMode.RightToLeft)
                TweenBack();
            else if (distanceToRight > distanceToLeft && mode == FlipMode.LeftToRight)
                TweenBack();
            else
                TweenForward();
        }

        private void UpdatePages()
        {
            _currentLeftPage = _bookPages[CurrentPage];
            SetPage(_currentLeftPage, _leftNextTransform);

            _currentRightPage = _bookPages[CurrentPage + 1];
            SetPage(_currentRightPage, _rightNextTransform);
        }

        private void TweenForward()
        {
            if (mode == FlipMode.RightToLeft)
                Timing.RunCoroutine(TweenTo(_edgeBottomLeft, 0.15f, Flip));
            else
                Timing.RunCoroutine(TweenTo(_edgeBottomRight, 0.15f, Flip));
        }

        private void Flip()
        {
            if (mode == FlipMode.RightToLeft)
                CurrentPage = Mathf.Clamp(CurrentPage + 2, 0, TotalPageCount - 1);
            else
                CurrentPage = Mathf.Clamp(CurrentPage - 2, 0, TotalPageCount - 1);

            _rightHotspot.enabled = CurrentPage < TotalPageCount - 3;
            _leftHotspot.enabled = CurrentPage > 0;

            _leftNextTransform.SetParent(_bookTransform, true);
            _leftTransform.SetParent(_bookTransform, true);
            _leftNextTransform.SetParent(_bookTransform, true);
            _leftNextTransform.SetAsFirstSibling();
            _leftTransform.gameObject.SetActive(false);
            _rightTransform.gameObject.SetActive(false);
            _rightTransform.SetParent(_bookTransform, true);
            _rightNextTransform.SetParent(_bookTransform, true);
            _rightNextTransform.SetAsFirstSibling();
            UpdatePages();
            _shadowTransform.gameObject.SetActive(false);
            _shadowLTRTransform.gameObject.SetActive(false);
            OnFlip?.Invoke();
        }

        private void TweenBack()
        {
            if (mode == FlipMode.RightToLeft)
            {
                Timing.RunCoroutine(TweenTo(_edgeBottomRight, 0.15f,
                    () =>
                    {
                        UpdatePages();
                        _rightNextTransform.SetParent(_bookTransform);
                        _rightNextTransform.SetAsFirstSibling();
                        _rightTransform.SetParent(_bookTransform);

                        _leftTransform.gameObject.SetActive(false);
                        _rightTransform.gameObject.SetActive(false);
                        pageDragging = false;
                    }
                ));
            }
            else
            {
                Timing.RunCoroutine(TweenTo(_edgeBottomLeft, 0.15f,
                    () =>
                    {
                        UpdatePages();

                        _leftNextTransform.SetParent(_bookTransform);
                        _leftNextTransform.SetAsFirstSibling();
                        _leftTransform.SetParent(_bookTransform);

                        _leftTransform.gameObject.SetActive(false);
                        _rightTransform.gameObject.SetActive(false);
                        pageDragging = false;
                    }
                ));
            }
        }

        private IEnumerator<float> TweenTo(Vector3 to, float duration, Action onFinish)
        {
            int steps = (int) (duration / 0.025f);
            Vector3 displacement = (to - _followPoint) / steps;

            for (int i = 0; i < steps - 1; i++)
            {
                if (mode == FlipMode.RightToLeft)
                    UpdateBookRTLToPoint(_followPoint + displacement);
                else
                    UpdateBookLTRToPoint(_followPoint + displacement);

                yield return Timing.WaitForSeconds(0.025f);
            }

            onFinish?.Invoke();
        }
    }
}