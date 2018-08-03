using System;
using System.Collections.Generic;
using System.Linq;

namespace BadgerEdit
{
    public abstract class MoveDirective
    {
        public abstract IntVector Execute(List<Line> lines, IntVector pos, params object[] parameters);

        public static MoveDirective Up = new UpClass();
        public static MoveDirective Down = new DownClass();
        public static MoveDirective Left = new LeftClass();
        public static MoveDirective Right = new RightClass();
        public static MoveDirective Home = new HomeClass();
        public static MoveDirective End = new EndClass();
        public static MoveDirective StartOfWord = new StartOfWordClass();
        public static MoveDirective EndOfWord = new EndOfWordClass();
        public static MoveDirective StartOfDocument = new StartOfDocumentClass();
        public static MoveDirective EndOfDocument = new EndOfDocumentClass();
        public static MoveDirective PageUp = new PageUpClass();
        public static MoveDirective PageDown = new PageDownClass();

        public class UpClass : MoveDirective
        {
            public override IntVector Execute(List<Line> lines, IntVector pos, params object[] parameters)
            {
                if (pos.Y <= 0)
                    return pos;

                pos.Y -= 1;
                if (lines[pos.Y].Count < pos.X)
                    pos.X = lines[pos.Y].Count;

                return pos;
            }
        }
        public class DownClass : MoveDirective
        {
            public override IntVector Execute(List<Line> lines, IntVector pos, params object[] parameters)
            {
                if (pos.Y >= lines.Count || pos.Y +1 >= lines.Count)
                    return pos;

                pos.Y += 1;
                
                if (lines[pos.Y].Count < pos.X)
                    pos.X = lines[pos.Y].Count;

                return pos;
            }
        }
        public class LeftClass : MoveDirective
        {
            public override IntVector Execute(List<Line> lines, IntVector pos, params object[] parameters)
            {
                if (pos.X <= 0)
                {
                    var newP = Up.Execute(lines, pos);
                    return End.Execute(lines, newP);
                }

                pos.X -= 1;
                return pos;
            }
        }
        public class RightClass : MoveDirective
        {
            public override IntVector Execute(List<Line> lines, IntVector pos, params object[] parameters)
            {
                var line = lines[pos.Y];

                if (pos.X >= line.Count)
                {
                    var newP = Down.Execute(lines, pos);
                    return Home.Execute(lines, newP);
                }

                pos.X += 1;
                return pos;
            }
        }

        public class StartOfDocumentClass : MoveDirective
        {
            public override IntVector Execute(List<Line> lines, IntVector pos, params object[] parameters)
            {
                pos.X = 0;
                pos.Y = 0;
                return pos;
            }
        }
        public class EndOfDocumentClass : MoveDirective
        {
            public override IntVector Execute(List<Line> lines, IntVector pos, params object[] parameters)
            {
                pos.Y = lines.Count;
                pos.X = lines[lines.Count-1].Count;

                return pos;
            }
        }
        public class HomeClass : MoveDirective
        {
            public override IntVector Execute(List<Line> lines, IntVector pos, params object[] parameters)
            {
                pos.X = 0;
                return pos;
            }
        }
        public class EndClass : MoveDirective
        {
            public override IntVector Execute(List<Line> lines, IntVector pos, params object[] parameters)
            {
                var line = lines[pos.Y];
                pos.X = line.Count;
                return pos;
            }
        }

        public class StartOfWordClass : MoveDirective
        {
            public override IntVector Execute(List<Line> lines, IntVector pos, params object[] parameters)
            {
                var line = lines[pos.Y];
                if (line.Count == 0)
                    return pos;
                if (line[Math.Max(0, pos.X - 1)].Character == ' ')
                {
                    pos.X -= 1;
                }
                for (int i = pos.X; i > 1; i--)
                {
                    if (line[i-1].Character == ' ')
                    {
                        pos.X = i;
                        return pos;
                    }
                }

                pos.X = 0;
                return pos;
            }
        }

        public class PageUpClass : MoveDirective
        {
            /// <summary>
            /// 
            /// </summary>
            /// <param name="lines"></param>
            /// <param name="pos"></param>
            /// <param name="parameters">
            /// First parameter : integer : index of first visible line
            /// Second parameter : integer : index of the last visible line
            /// </param>
            /// <returns></returns>
            public override IntVector Execute(List<Line> lines, IntVector pos, params object[] parameters) {

                if(parameters.Length < 2)
                {
                    throw new ArgumentException("please pass two INT arguments; index of first and last line visible");
                }

                if(parameters.ToList().Any(p => p is int == false))
                {
                    throw new ArgumentException("pass only integers.");
                }

                int firstLine = (int)parameters[0];
                int lastLine = (int)parameters[1];

                if (pos.Y == firstLine)
                {
                    pos.Y = Math.Max(0, pos.Y - Math.Abs(lastLine - firstLine));
                    return pos;
                }
                else
                {
                    pos.Y = firstLine;
                    return pos;
                }
            }
        }

        public class PageDownClass : MoveDirective
        {
            /// <summary>
            /// 
            /// </summary>
            /// <param name="lines"></param>
            /// <param name="pos"></param>
            /// <param name="parameters">
            /// First parameter : integer : index of first visible line
            /// Second parameter : integer : index of the last visible line
            /// </param>
            /// <returns></returns>
            public override IntVector Execute(List<Line> lines, IntVector pos, params object[] parameters)
            {

                if (parameters.Length < 2)
                {
                    throw new ArgumentException("please pass two INT arguments; index of first and last line visible");
                }

                if (parameters.ToList().Any(p => p is int == false))
                {
                    throw new ArgumentException("pass only integers.");
                }

                int firstLine = (int)parameters[0];
                int lastLine = (int)parameters[1];

                if (pos.Y == lastLine)
                {
                    pos.Y = Math.Min(lines.Count-1, pos.Y + Math.Abs(lastLine - firstLine));
                    return pos;
                }
                else
                {
                    pos.Y = lastLine;
                    return pos;
                }
            }
        }

        public class EndOfWordClass : MoveDirective
        {
            public override IntVector Execute(List<Line> lines, IntVector pos, params object[] parameters)
            {
                var line = lines[pos.Y];
                if (line.Count == 0)
                    return pos;
                if (line[Math.Max(0,Math.Min(line.Count-1, pos.X))].Character == ' ')
                {
                    pos.X += 1;
                }
                for (int i = pos.X; i < line.Count; i++)
                {
                    if (line[i].Character == ' ')
                    {
                        pos.X = i;
                        return pos;
                    }
                }

                pos.X = line.Count;
                return pos;
            }
        }
    }
}