using System;
using System.Collections.Generic;
using System.Text;

namespace TGREdit
{
    public struct BreakPoint
    {
        public int mLine;
        public bool mEnabled;
        public string mCondition;

        BreakPoint(int line = -1, string condition = "", bool enabled = false)
        {
            mLine = line;
            mCondition = condition;
            mEnabled = enabled;
        }
    }
}
