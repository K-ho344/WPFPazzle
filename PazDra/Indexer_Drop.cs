using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static PazDra.PazDraConstants;

namespace PazDra
{
    internal class Indexer_Drop
    {
        private readonly string[,] DropElem = new string[WIDTH, HEIGHT];
        public string this[int X, int Y]
        {
            set => DropElem[X, Y] = value;
            get => DropElem[X, Y];
        }
    }
}
