using System;
using System.Windows;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Media.Animation;
using static PazDra.PazDraConstants;

namespace PazDra
{
    // パズル画面上に表示される色のついた丸いピースのことを"ドロップ"と呼ぶ
    internal class DropBoard : INotifyPropertyChanged
    {
        private const int DERAY = 300; //ドロップ消去・再生成後の遅延時間

        public static readonly string[] DropElements = new string[]
        {
            // ドロップは5種類（NONEはドロップがないマスを表す）
            AQUA,DARK,FLAME,LIGHT,WOOD,NONE
        };

        //クリックしたドロップの見た目を変える
        public static readonly Dictionary<string, string> dic_Clicked = new Dictionary<string, string>()
        {
            {AQUA,AQUA_CLICK },{DARK,DARK_CLICK},
            {FLAME,FLAME_CLICK },{LIGHT,LIGHT_CLICK},{WOOD,WOOD_CLICK},
            {NONE,NONE}//NONEをクリックしたときにエラーが出ないように
        };

        private bool[,] toErace = new bool[WIDTH, HEIGHT]; //3つ以上そろったドロップの削除フラグ
        private bool[,] isEmpty = new bool[WIDTH, HEIGHT]; //削除した位置に上のドロップを落下させるためのフラグ
        private bool[,] toGenerate = new bool[WIDTH, HEIGHT];　//上に落下するドロップがない場合の生成フラグ

        private static readonly Random rnd = new Random();

        //ドラッグしているドロップを常に表示するために、表示するドロップを以下の2層に分ける

        //実際につかんで移動させるためのドロップ(xamlではThumb要素にBinding)
        public Indexer_Drop Drop { get; } = new Indexer_Drop();
        //ドラッグ中に背景に表示するドロップ(xamlではImage要素にBinding)
        public Indexer_Drop Drop_BG { get; } = new Indexer_Drop(); 

        //ドラッグのマウスカーソルがいる行・列を保持
        public int DraggingColumn;　
        public int DraggingRow;

        private string DraggingDropElem;

        #region INotifyPropertyChanged の実装
        public event PropertyChangedEventHandler PropertyChanged;
        private void RaiseProeprtyChanged(string propertyName)
        {
            var h = this.PropertyChanged;
            if (h != null) h(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion INotifyPropertyChanged の実装

        public DropBoard()
        {
            for (int col = 0; col < WIDTH; col++)
            {
                for (int row = 0; row < HEIGHT; row++)
                {
                    Drop[col,row] = DropElements[rnd.Next(DropElements.Length - 1)];
                    Drop_BG[col,row] = Drop[col,row];
                    toErace[col,row] = false;
                    isEmpty[col,row] = false;
                    toGenerate[col,row] = false;
                }
            }
        }

        public void SwapColumn(int movedhrzn)
        {

            if ((DraggingColumn < 0) || (DraggingColumn > 5)
                || ((DraggingColumn + movedhrzn) < 0) || ((DraggingColumn + movedhrzn) > 5))
            {
                //Swapはしないがマウスカーソルのある列は保持する
                DraggingColumn = DraggingColumn + movedhrzn;　
                System.Diagnostics.Debug.WriteLine("Swapキャンセル");
                return;
            }

            //マウスカーソルが盤面外に出ている場合の対応
            int temp_DraggingRow;
            if ((DraggingRow < 0))
            {
                temp_DraggingRow = 0;
            }
            else if (DraggingRow > 4)
            {
                temp_DraggingRow = 4;
            }
            else
            {
                temp_DraggingRow = DraggingRow;
            }

            string temp_DropElem = Drop_BG[DraggingColumn + movedhrzn, temp_DraggingRow];
            Drop_BG[DraggingColumn + movedhrzn, temp_DraggingRow] = Drop_BG[DraggingColumn, temp_DraggingRow];
            RaiseProeprtyChanged("Drop_BG");

            Drop_BG[DraggingColumn, temp_DraggingRow] = temp_DropElem;
            RaiseProeprtyChanged("Drop_BG");

            DraggingColumn = DraggingColumn + movedhrzn;
        }

        public void SwapRow(int movedvrt)
        {

            if ((DraggingRow < 0) || (DraggingRow > 4)
                || ((DraggingRow + movedvrt) < 0 || (DraggingRow + movedvrt) > 4))
            {
                //Swapはしないがマウスカーソルのある行は保持する
                DraggingRow = DraggingRow + movedvrt;
                System.Diagnostics.Debug.WriteLine("Swapキャンセル");
                return;
            }

            //マウスカーソルが盤面外に出ている場合の対応
            int temp_DraggingColumn;
            if (DraggingColumn < 0)
            {
                temp_DraggingColumn = 0;
            }
            else if (DraggingColumn > 5)
            {
                temp_DraggingColumn = 5;
            }
            else
            {
                temp_DraggingColumn = DraggingColumn;
            }

            string temp_DropElem = Drop_BG[temp_DraggingColumn, DraggingRow + movedvrt];
            Drop_BG[temp_DraggingColumn, DraggingRow + movedvrt] = Drop_BG[temp_DraggingColumn, DraggingRow];
            RaiseProeprtyChanged("Drop_BG");

            Drop_BG[temp_DraggingColumn, DraggingRow] = temp_DropElem;
            RaiseProeprtyChanged("Drop_BG");

            DraggingRow = DraggingRow + movedvrt;
        }

        public async Task MakeCombo()
        {
            SetDragCompletedDropBG();
            await EraceChain();
            Animation_FallDownDrops();
        }

        private bool CheckChain()
        {
            bool needErace = false;
            for (int col = 0; col < WIDTH; col++)
            {
                for (int row = 0; row < HEIGHT; row++)
                {
                    if (row + 2 < HEIGHT)
                    {
                        if (Drop_BG[col, row] == Drop_BG[col, row + 1])
                        {
                            if (Drop_BG[col, row] == Drop_BG[col, row + 2])
                            {
                                toErace[col, row] = true;
                                toErace[col, row + 1] = true;
                                toErace[col, row + 2] = true;
                                needErace = true;
                            }
                        }
                    }

                    if (col + 2 < WIDTH)
                    {
                        if (Drop_BG[col, row] == Drop_BG[col + 1, row])
                        {
                            if (Drop_BG[col, row] == Drop_BG[col + 2, row])
                            {
                                toErace[col, row] = true;
                                toErace[col + 1, row] = true;
                                toErace[col + 2, row] = true;
                                needErace = true;
                            }
                        }
                    }
                }
            }
            return needErace;
        }

        private async Task EraceChain()
        {
            if (!CheckChain())
                return;
            for (int col = 0; col < WIDTH; col++)
            {
                for (int row = 0; row < HEIGHT; row++)
                {
                    if (toErace[col, row] == true)
                    {
                        Drop_BG[col, row] = NONE;
                        Drop[col, row] = NONE;
                        isEmpty[col, row] = true;
                        toGenerate[col, row] = true;
                        toErace[col, row] = false;
                    }
                }
            }
            RaiseProeprtyChanged("Drop_BG");
            RaiseProeprtyChanged("Drop");
            await Task.Delay(DERAY);
            Animation_FallDownDrops();//まず落下アニメーションを再生する
        }

        private void Animation_FallDownDrops()
        {
            List<string> list_sb = new List<string>();

            for (int row = 0; row < (HEIGHT - 1); row++)
            {
                for (int col = 0; col < WIDTH; col++)
                {
                    int fallcount = 0;
                    if (!isEmpty[col, row])
                    {
                        for (int k = (row + 1); k < HEIGHT; k++)
                        {
                            if (isEmpty[col, k])
                                fallcount++;
                        }
                        if (fallcount != 0)
                        {
                            //xamlに定義した落下アニメーションのStoryBoardのstringリストを作る
                            string str = ("FallingCell" + fallcount + "_" + col + row);
                            list_sb.Add(str);
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("This Drop is NONE, Canceled");
                        }
                    }
                }
            }
            PlayFallDownStoryBoards(list_sb);
        }

        private void PlayFallDownStoryBoards(List<string> list_sb)
        {
            if (list_sb.Count == 0)
            {
                SpawnDropsAtNONECell();
                return;
            }
            for (int i = 0; i < list_sb.Count; i++)
            {
                Window window = Application.Current.MainWindow;
                var Fall_sb = window.FindResource(list_sb[i]) as Storyboard;

                if (i == (list_sb.Count - 1))
                {
                    Fall_sb.Completed += Fall_Completed;
                    /* リストの最後のStoryBoardが再生完了したときのイベントハンドラ
                    　 ただし、Listの最後のアニメーションが終了したとしても、
                       すべてのアニメーションが終了しているとは限らない。
                       そのため、アニメーションが途中で途切れるという不具合が発生している。
                       すべてのアニメーションが完了したことを知る方法がなく、解決不能。
                    */
                }

                window.BeginStoryboard(Fall_sb);
                System.Diagnostics.Debug.WriteLine(list_sb[i] + "再生");
            }
        }

        private async void Fall_Completed(object sender, EventArgs e)
        {
            await FallDownDropBG();//落下アニメーションが終了したらDropBGを変化させる
            System.Diagnostics.Debug.WriteLine("FallDownAnimationCompleted");
        }

        private async Task FallDownDropBG()
        {
            for (int row = (HEIGHT-2); row >= 0; row--)
            {
                for (int col = 0; col < WIDTH; col++)
                {
                    int fallcount = 0;
                    if (!isEmpty[col, row])
                    {
                        for (int k = (row + 1); k < HEIGHT; k++)
                        {
                            if (isEmpty[col, k])
                                fallcount++;   //下に何個空きがあるか計算
                        }
                        if (fallcount != 0)
                        {
                            Drop_BG[col, row + fallcount] = Drop_BG[col, row];
                            Drop[col, row + fallcount] = Drop_BG[col, row];
                            isEmpty[col, row + fallcount] = false;
                            toGenerate[col, row + fallcount] = false;
                            Drop_BG[col, row] = NONE;
                            Drop[col, row] = NONE;
                            isEmpty[col, row] = true;
                            toGenerate[col, row] = true;
                            System.Diagnostics.Debug.WriteLine("FallingAfterAnimation...");
                        }
                    }
                }
            }
            RaiseProeprtyChanged("Drop_BG");
            RaiseProeprtyChanged("Drop");
            await Task.Delay(DERAY);
            SpawnDropsAtNONECell();
        }

        private void SpawnDropsAtNONECell()
        {
            List<string> lst_str = new List<string>();
            for (int col = 0; col < WIDTH; col++)
            {
                for (int row = 0; row < HEIGHT; row++)
                {
                    if (toGenerate[col, row])
                    {
                        Drop_BG[col, row] = DropElements[rnd.Next(DropElements.Length - 1)];

                        //xamlに定義した生成アニメーションのStoryBoardのstringリストを作る
                        string str = "FadeIn" + col + row;
                        lst_str.Add(str);
                        isEmpty[col, row] = false;
                        toGenerate[col, row] = false;
                        toErace[col, row] = false;
                    }
                }
            }
            RaiseProeprtyChanged("Drop_BG");
            PlayFadeinStoryBoards(lst_str);
        }

        private void PlayFadeinStoryBoards(List<string> lst_str)
        {
            Storyboard Fade_sb;
            for (int i = 0; i<lst_str.Count; i++)
            {
                Window window = Application.Current.MainWindow;
                Fade_sb = window.FindResource(lst_str[i]) as Storyboard;
                if(i == (lst_str.Count-1))
                {
                    Fade_sb.Completed += Fade_Completed;
                }
                window.BeginStoryboard(Fade_sb);
            }
        }

        private async void Fade_Completed(object sender, EventArgs e)
        {
            SyncDropToDrop_BG();

            //コンボ開始
            if (CheckChain())
            {
                await EraceChain();
            }
        }

        public void SyncDropToDrop_BG()
        {
            for (int j = 0; j < HEIGHT; j++)
            {
                for (int i = 0; i < WIDTH; i++)
                {
                    Drop[i, j] = Drop_BG[i, j];
                }
            }
            RaiseProeprtyChanged("Drop");
        }

        public void SetDragCompletedDropBG()
        {
            int temp_DraggingColumn, temp_DraggingRow;
            if ((DraggingRow < 0))
            {
                temp_DraggingRow = 0;
            }
            else if (DraggingRow > 4)
            {
                temp_DraggingRow = 4;
            }
            else
            {
                temp_DraggingRow = DraggingRow;
            }

            if (DraggingColumn < 0)
            {
                temp_DraggingColumn = 0;
            }
            else if (DraggingColumn > 5)
            {
                temp_DraggingColumn = 5;
            }
            else
            {
                temp_DraggingColumn = DraggingColumn;
            }
            Drop_BG[temp_DraggingColumn, temp_DraggingRow] = DraggingDropElem;
            RaiseProeprtyChanged("Drop_BG");
        }

        //前面のドロップをドラッグ中のもの以外透明にする。
        public void MakeAllDropNONEWithoutClickedOne(int clkdCol, int clkdRow)
        {
            for (int j = 0; j < HEIGHT; j++)
            {
                for (int i = 0; i < WIDTH; i++)
                {
                    if (i == clkdCol && j == clkdRow)
                    {
                        DraggingDropElem = Drop[clkdCol, clkdRow];
                        continue;
                    }
                    Drop[i, j] = NONE;
                }
            }
            //ドラッグ中のドロップは色を薄く（InvisibleDropに）する。
            DropMakeInvisible(clkdCol, clkdRow);
        }

        private void DropMakeInvisible(int column, int row)
        {
            Drop[column, row] = dic_Clicked[Drop[column, row]]; //Drop_BGは変化不要。
            RaiseProeprtyChanged("Drop");
        }

        //ドラッグ中の背面のDrop_BGは空白にする。（前面のDropをドラッグ中のため）
        public void ClickedDropBGMakeNONE(int column, int row)
        {
            Drop_BG[column, row] = NONE;
            RaiseProeprtyChanged("Drop_BG");
        }

        public void DragCompletedDropMakeNONE(int col, int row)
        {
            Drop[col, row] = NONE;
            RaiseProeprtyChanged("Drop");
        }
    }
}





