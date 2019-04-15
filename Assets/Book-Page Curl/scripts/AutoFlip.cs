using System.Collections;
using UnityEngine;

namespace FrogunnerGames.UI.Book
{
    [RequireComponent(typeof(Book))]
    public class AutoFlip : MonoBehaviour
    {
        public FlipMode Mode;
        public float PageFlipTime = 1;
        public float TimeBetweenPages = 1;
        public float DelayBeforeStarting;
        public bool AutoStartFlip = true;
        public Book ControledBook;
        public int AnimationFramesCount = 40;

        private bool isFlipping;

        // Use this for initialization
        private void Start()
        {
            if (!ControledBook)
                ControledBook = GetComponent<Book>();
            if (AutoStartFlip)
                StartFlipping();
            ControledBook.OnFlip.AddListener(PageFlipped);
        }

        private void PageFlipped()
        {
            isFlipping = false;
        }

        public void StartFlipping()
        {
            StartCoroutine(FlipToEnd());
        }

        public void FlipRightPage()
        {
            if (isFlipping) return;
            if (ControledBook.CurrentPage >= ControledBook.TotalPageCount) return;
            isFlipping = true;
            float frameTime = PageFlipTime / AnimationFramesCount;
            float xc = (ControledBook.EndBottomRight.x + ControledBook.EndBottomLeft.x) / 2;
            float xl = ((ControledBook.EndBottomRight.x - ControledBook.EndBottomLeft.x) / 2) * 0.9f;
            //float h =  ControledBook.Height * 0.5f;
            float h = Mathf.Abs(ControledBook.EndBottomRight.y) * 0.9f;
            float dx = (xl) * 2 / AnimationFramesCount;
            StartCoroutine(FlipRTL(xc, xl, h, frameTime, dx));
        }

        public void FlipLeftPage()
        {
            if (isFlipping) return;
            if (ControledBook.CurrentPage <= 0) return;
            isFlipping = true;
            float frameTime = PageFlipTime / AnimationFramesCount;
            float xc = (ControledBook.EndBottomRight.x + ControledBook.EndBottomLeft.x) / 2;
            float xl = ((ControledBook.EndBottomRight.x - ControledBook.EndBottomLeft.x) / 2) * 0.9f;
            //float h =  ControledBook.Height * 0.5f;
            float h = Mathf.Abs(ControledBook.EndBottomRight.y) * 0.9f;
            float dx = (xl) * 2 / AnimationFramesCount;
            StartCoroutine(FlipLTR(xc, xl, h, frameTime, dx));
        }

        private IEnumerator FlipToEnd()
        {
            yield return new WaitForSeconds(DelayBeforeStarting);
            float frameTime = PageFlipTime / AnimationFramesCount;
            float xc = (ControledBook.EndBottomRight.x + ControledBook.EndBottomLeft.x) / 2;
            float xl = ((ControledBook.EndBottomRight.x - ControledBook.EndBottomLeft.x) / 2) * 0.9f;
            //float h =  ControledBook.Height * 0.5f;
            float h = Mathf.Abs(ControledBook.EndBottomRight.y) * 0.9f;
            //y=-(h/(xl)^2)*(x-xc)^2          
            //               y         
            //               |          
            //               |          
            //               |          
            //_______________|_________________x         
            //              o|o             |
            //           o   |   o          |
            //         o     |     o        | h
            //        o      |      o       |
            //       o------xc-------o      -
            //               |<--xl-->
            //               |
            //               |
            float dx = (xl) * 2 / AnimationFramesCount;
            switch (Mode)
            {
                case FlipMode.RightToLeft:
                    while (ControledBook.CurrentPage < ControledBook.TotalPageCount)
                    {
                        StartCoroutine(FlipRTL(xc, xl, h, frameTime, dx));
                        yield return new WaitForSeconds(TimeBetweenPages);
                    }

                    break;
                case FlipMode.LeftToRight:
                    while (ControledBook.CurrentPage > 0)
                    {
                        StartCoroutine(FlipLTR(xc, xl, h, frameTime, dx));
                        yield return new WaitForSeconds(TimeBetweenPages);
                    }

                    break;
            }
        }

        private IEnumerator InitialFlip()
        {
            yield return new WaitForSeconds(DelayBeforeStarting);
            float frameTime = PageFlipTime / AnimationFramesCount;
            float xc = (ControledBook.EndBottomRight.x + ControledBook.EndBottomLeft.x) / 2;
            float xl = ((ControledBook.EndBottomRight.x - ControledBook.EndBottomLeft.x) / 2) * 0.9f;
            //float h =  ControledBook.Height * 0.5f;
            float h = Mathf.Abs(ControledBook.EndBottomRight.y) * 0.9f;
            float dx = (xl) * 2 / AnimationFramesCount;

            if (ControledBook.StartingPage > ControledBook.CurrentPage)
                while (ControledBook.CurrentPage < ControledBook.TotalPageCount)
                {
                    {
                        StartCoroutine(FlipRTL(xc, xl, h, frameTime, dx));
                        yield return new WaitForSeconds(TimeBetweenPages);
                    }
                }
            else if (ControledBook.StartingPage < ControledBook.CurrentPage)
            {
                while (ControledBook.CurrentPage > 0)
                {
                    StartCoroutine(FlipLTR(xc, xl, h, frameTime, dx));
                    yield return new WaitForSeconds(TimeBetweenPages);
                }
            }
        }

        private IEnumerator FlipRTL(float xc, float xl, float h, float frameTime, float dx)
        {
            float x = xc + xl;
            float y = (-h / (xl * xl)) * (x - xc) * (x - xc);
            ControledBook.DragRightPageToPoint(new Vector3(x, y, 0));
            for (int i = 0;
                i < AnimationFramesCount;
                i++)
            {
                y = (-h / (xl * xl)) * (x - xc) * (x - xc);
                ControledBook.UpdateBookRTLToPoint(new Vector3(x, y, 0));
                yield return new WaitForSeconds(frameTime);
                x -= dx;
            }

            ControledBook.ReleasePage();
        }

        private IEnumerator FlipLTR(float xc, float xl, float h, float frameTime, float dx)
        {
            float x = xc - xl;
            float y = (-h / (xl * xl)) * (x - xc) * (x - xc);
            ControledBook.DragLeftPageToPoint(new Vector3(x, y, 0));
            for (int i = 0;
                i < AnimationFramesCount;
                i++)
            {
                y = (-h / (xl * xl)) * (x - xc) * (x - xc);
                ControledBook.UpdateBookLTRToPoint(new Vector3(x, y, 0));
                yield return new WaitForSeconds(frameTime);
                x += dx;
            }

            ControledBook.ReleasePage();
        }
    }
}