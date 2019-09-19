using System;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;

namespace AsyncTestForm
{
    public partial class Form1 : Form
    {
        /// <summary>
        /// Mutexの生成
        /// </summary>
        private Mutex mut = new Mutex();

        /// <summary>
        /// Taskのキャンセルトークンの生成
        /// </summary>
        private CancellationTokenSource cancellation = new CancellationTokenSource();

        /// <summary>
        /// Form1のコンストラクタ
        /// </summary>
        public Form1()
        {
            InitializeComponent();
            //念のために中央に表示
            StartPosition = FormStartPosition.CenterScreen;
        }

        /// <summary>
        /// button1からのスレッド起動
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void button1_Click(object sender, EventArgs e)
        {
            MessageOutput("-- " + sender.ToString() + "Click --");

            //button2が先に起動していたらキャンセルする
            CancelTask();

            //cancellationを生成。usingを使ってTask.runが終了したらDisposeする。
            using (cancellation = new CancellationTokenSource())
            {
                await Task.Run(() => TestThread(sender, cancellation.Token));
            }
        }

        /// <summary>
        /// Button2からのスレッド起動
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void button2_Click(object sender, EventArgs e)
        {
            MessageOutput("-- " + sender.ToString() + "Click --");

            //button1が先に起動していたらキャンセルする
            CancelTask();

            //cancellationを生成。usingを使ってTask.runが終了したらDisposeする。
            using (cancellation = new CancellationTokenSource())
            {
                await Task.Run(() => TestThread(sender, cancellation.Token));
            }
        }

        /// <summary>
        /// 実行中のtaskをキャンセルする。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button3_Click(object sender, EventArgs e)
        {
            CancelTask();
        }

        /// <summary>
        /// WInformに画面UIスレッドと別のスレッドからアクセスするためのメソッド
        /// </summary>
        /// <param name="message"></param>
        private void MessageOutput(string message)
        {
            //別スレッドからの呼ばれた場合
            if(InvokeRequired)
            {
                //Dispatcher.InvokeでDispatcherのキューにメソッドを突っ込む
                //MessageOutputをキューに突っ込むだけをして終了
                Invoke((Action)(() => MessageOutput(message)));
                return;
            }
            //Dispatcherのキューに入っている順番にMessage出力
            //画面スレッドが優先
            textBox1.Text += message + Environment.NewLine;
        }

        /// <summary>
        /// 適当なメソッド。1秒ごとに文字列を吐き出す
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="token"></param>
        private void TestThread(object sender,CancellationToken token)
        {
            //Mutexの取得。
            //別スレッドからこのメソッドが呼ばれたときは、Releaseされるまで止める。
            mut.WaitOne();
            MessageOutput("-- " + sender.ToString() + "START --");

            int i = 0;

            for (; ; )
            {
                if(token.IsCancellationRequested)
                {
                    MessageOutput("-- " + sender.ToString() + "Cancel --");
                    break;
                }
                MessageOutput(sender.ToString() + " : count " + i++);
                Thread.Sleep(1000);
            }
            MessageOutput("-- " + sender.ToString() + "END --");
            mut.ReleaseMutex();
            //Mutexの解放
        }


        /// <summary>
        /// 実行中のタスクのキャンセル
        /// </summary>
        private void CancelTask()
        {
            //cancellationTokenをキャンセルする。
            try
            {
                cancellation.Cancel();
                MessageOutput("-- Task Cancel --");

            }
            catch (ObjectDisposedException)
            {
                //cancellationTokenがDisposeされている場合の例外処理
                //cancellationTokenがDisposeの確認方法がこれしかわからない
                MessageOutput("-- ObjectDisposedException throw --");
            }
        }

    }
}
