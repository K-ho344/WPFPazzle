using System.Windows;
using System.Windows.Controls;

namespace PazDra
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int SIZE_CELL = 60;
        DropBoard board = new DropBoard();
        private double Sum_VChange = 0;
        private double Sum_HChange = 0;
        private double Tmp_VChange = 0;
        private double Tmp_HChange = 0;
        private double pos_Vrtcl = 0;
        private double pos_Hrzntl = 0;
        private int ClickedRow;
        private int ClickedColumn;
        System.Windows.Controls.Primitives.Thumb ClickedDrop;

        public MainWindow() => InitializeComponent();

        private void button_Click_START(object sender, RoutedEventArgs e)
        {
            board = new DropBoard();
            DataContext = board;
        }

        /// <summary>
        /// ドラッグ開始
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mark_DragStarted(object sender,
            System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            ClickedDrop = (System.Windows.Controls.Primitives.Thumb)sender;
            System.Diagnostics.Debug.WriteLine("★★ Thumb:" + ClickedDrop.Name + "をドラッグ開始" + "★★");
            pos_Vrtcl = Canvas.GetTop(ClickedDrop);
            pos_Hrzntl = Canvas.GetLeft(ClickedDrop);

            //疑似的に1セル：SIZE_CELL*SIZE_CELLのグリッドを想定し、グリッド上の位置を特定
            ClickedRow = (int)pos_Vrtcl / SIZE_CELL;
            ClickedColumn = (int)pos_Hrzntl / SIZE_CELL;

            board.DraggingColumn = ClickedColumn;
            board.DraggingRow = ClickedRow;

            //クリックした箇所以外の前面のドロップを不可視化、背面のDrop_BGだけが見えている状態にする
            board.MakeAllDropNONEWithoutClickedOne(ClickedColumn, ClickedRow);
            //クリックした箇所の背面のDrop_BGは見えなくする（ドラッグ中の前面のDropとしては見えている）
            board.ClickedDropBGMakeNONE(ClickedColumn, ClickedRow);
        }

        /// <summary>
        /// ドラッグ中
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mark_DragDelta(object sender,
            System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            Canvas.SetTop(ClickedDrop, Canvas.GetTop(ClickedDrop) + e.VerticalChange);
            Canvas.SetLeft(ClickedDrop, Canvas.GetLeft(ClickedDrop) + e.HorizontalChange);

            Sum_VChange += (e.VerticalChange);
            Sum_HChange += (e.HorizontalChange);

            int Delta_Vrtcl = (int)(Sum_VChange - Tmp_VChange);
            int Delta_Row = Delta_Vrtcl / SIZE_CELL;
            int Delta_Hrzntl = (int)(Sum_HChange - Tmp_HChange);
            int Delta_Column = Delta_Hrzntl / SIZE_CELL;

            System.Diagnostics.Debug.WriteLine("Sum_VChange: " + Sum_VChange);
            System.Diagnostics.Debug.WriteLine("Tmp_VChange: " + Tmp_VChange);

            if (Delta_Row >= 1 || Delta_Row <= -1)
            {
                System.Diagnostics.Debug.WriteLine("SwapRow呼び出し");
                board.SwapRow(Delta_Row);
                Tmp_VChange = Sum_VChange;
            }

            if (Delta_Column >= 1 || Delta_Column <= -1)
            {
                System.Diagnostics.Debug.WriteLine("SwapColumn呼び出し");
                board.SwapColumn(Delta_Column);
                Tmp_HChange = Sum_HChange;
            }
        }

        /// <summary>
        /// ドラッグ終了
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void mark_DragCompleted(object sender,
            System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            Canvas.SetTop(ClickedDrop, pos_Vrtcl);
            Canvas.SetLeft(ClickedDrop, pos_Hrzntl);

            //ドラッグ終了後、ドラッグしていた前面のDropは見えなくする
            board.DragCompletedDropMakeNONE(ClickedColumn, ClickedRow);
            await board.MakeCombo();
            board.SyncDropToDrop_BG();
            InitializeField();
        }

        private void InitializeField()
        {
            Sum_VChange = 0;
            Sum_HChange = 0;
            Tmp_VChange = 0;
            Tmp_HChange = 0;
        }
    }
}
