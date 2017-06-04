using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Automation;

namespace TwitchCommentReader
{
    public class Una
    {
        AutomationElement unaWindowElement;
        AutomationElement playButtonElement;
        InvokePattern playButtonInvoker;

        IntPtr messageBoxHandle;

        ConcurrentQueue<string> playMessageQueue = new ConcurrentQueue<string>();

        // RichEdit にテキストを流し込むには SendMessage しかない
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, string lParam);

        const uint WM_SETTEXT = 0x000C;

        bool IsPlaying => playButtonElement.Current.Name.Contains("一時停止");
        void SetMessageToBox(string message) => SendMessage(messageBoxHandle, WM_SETTEXT, IntPtr.Zero, message);
        void ClickPlayButton() => playButtonInvoker.Invoke();

        public Una()
        {
            var windowElements = AutomationElement.RootElement.FindAll(TreeScope.Children, Condition.TrueCondition).Cast<AutomationElement>();

            foreach (var windowElement in windowElements)
            {
                if (windowElement.Current.Name.StartsWith("音街ウナTalk Ex"))
                {
                    unaWindowElement = windowElement;
                }
            }

            if (unaWindowElement == null)
            {
                throw new Exception("音街ウナが見つかりませんでした");
            }

            FindTargetUIElements();
            PlayQueueOneByOne().ConfigureAwait(false);
        }

        void FindTargetUIElements()
        {
            var childElements = unaWindowElement.FindAll(TreeScope.Element | TreeScope.Descendants, Condition.TrueCondition).Cast<AutomationElement>();
            var messageBoxWillCome = false;

            foreach (var childElement in childElements)
            {
                try
                {
                    var childInfo = childElement.Current;
                    Debug.WriteLine(childInfo.Name + " " + childInfo.AutomationId);

                    if (messageBoxWillCome)
                    {
                        messageBoxHandle = new IntPtr(childInfo.NativeWindowHandle);
                        messageBoxWillCome = false;
                    }
                    else if (childInfo.AutomationId == "txtMain")
                    {
                        messageBoxWillCome = true;
                    }
                    else if (childInfo.AutomationId == "btnPlay")
                    {
                        playButtonElement = childElement;
                        playButtonInvoker = (InvokePattern)playButtonElement.GetCurrentPattern(InvokePattern.Pattern);
                    }
                }
                catch
                {
                }
            }
        }

        public void Talk(string id, string message)
        {
            playMessageQueue.Enqueue($"{id}さん、{message}");
        }

        async Task PlayQueueOneByOne()
        {
            while (true)
            {
                if (!IsPlaying && playMessageQueue.TryDequeue(out string message))
                {
                    SetMessageToBox(message);
                    ClickPlayButton();

                    while (!IsPlaying) await Task.Delay(100);
                }

                await Task.Delay(100);
            }
        }
    }
}
