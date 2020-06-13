using System;
using System.Drawing;
using System.Linq;
using System.Media;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Windows;
using System.Xml.Serialization;
using Xceed.Wpf.Toolkit.PropertyGrid.Editors;
using Point = System.Windows.Point;

namespace Rasterizer
{
    [Serializable]
    public class Faccia : Nodo
    {
        static int nNome = 0;
        public string Nome { get; } = Vettore.StringaLettereDaNumero(nNome++).ToLower();
        public Vettore[] Vertici { get; } = new Vettore[3];

        [NonSerialized]
        private Color colore = Color.DodgerBlue;
        private string coloreString;

        public System.Windows.Media.Color Colore
        {
            get { return System.Windows.Media.Color.FromArgb(colore.A, colore.R, colore.G, colore.B); }
            set { colore = Color.FromArgb(value.A, value.R, value.G, value.B); }
        }

        [OnSerializing()]
        internal void OnSerializingMethod(StreamingContext context)
        {
            coloreString = Colore.ToString();
        }

        [OnDeserialized()]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            Colore = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(coloreString);
            nNome++;
        }

        internal void Disegnati(Bitmap bmp, double[,] zBuffer, Camera camera)
        {
            if (Vertici.All(v => v.z < 0) || Vertici.Any(v => v == null) || !FacciaRivoltaATelecamera())
                return;

            GraphicsUnit u = GraphicsUnit.Pixel;

            Point min = new Point(LimitaAViewport(camera, Vertici.Min(v => v.x)), LimitaAViewport(camera, Vertici.Min(v => v.y)));
            Point max = new Point(LimitaAViewport(camera, Vertici.Max(v => v.x)), LimitaAViewport(camera, Vertici.Max(v => v.y)));
            
            for (double x = min.X; x <= max.X; x++)
            {
                for (double y = min.Y; y <= max.Y; y++)
                {
                    var p = new Point(x, y);

                    if (puntoInTriangolo(p))
                    {
                        int Xi = (int)Math.Round(p.X + (camera.Rx / 2.0));
                        int Yi = (int)Math.Round(-p.Y + (camera.Ry / 2.0));

                        if (bmp.GetBounds(ref u).Contains(Xi, Yi))
                        {
                            var z = CalcolaZ(p);

                            if (z > 0.1 && z < zBuffer[Xi, Yi])
                            {
                                bmp.SetPixel(Xi, Yi, colore);
                                zBuffer[Xi, Yi] = z;
                            }
                        }
                    }
                }
            }
        }

        private double LimitaAViewport(Camera camera, double n)
        {
            return Math.Min(Math.Max(-camera.Rx / 2.0, n), camera.Rx / 2.0);
        }

        double CalcolaZ(Point p)
        {
            int i = 0;
            return 1 / Vertici.Sum(v => 1 / v.z * λ(i++, p));
        }

        bool FacciaRivoltaATelecamera()
        {
            return 0 < (Vertici[1].y - Vertici[0].y) * (Vertici[2].x - Vertici[1].x) - (Vertici[1].x - Vertici[0].x) * (Vertici[2].y - Vertici[1].y);
        }

        double edgeFunction(Point a, Point b, Point c)
        {
            return (c.X - a.X) * (b.Y - a.Y) - (c.Y - a.Y) * (b.X - a.X);
        }

        bool puntoInTriangolo(Point p)
        {
            VerticiInPunti(out Point p0, out Point p1, out Point p2);

            if (edgeFunction(p1, p2, p) < 0)
                return false;
            if (edgeFunction(p2, p0, p) < 0)
                return false;
            if (edgeFunction(p0, p1, p) < 0)
                return false;

            return true;
        }

        private void VerticiInPunti(out Point p0, out Point p1, out Point p2)
        {
            p0 = new Point(Vertici[0].x, Vertici[0].y);
            p1 = new Point(Vertici[1].x, Vertici[1].y);
            p2 = new Point(Vertici[2].x, Vertici[2].y);
        }

        double λ(int vertice, Point p)
        {
            VerticiInPunti(out Point p0, out Point p1, out Point p2);

            double numeratore = 0;

            switch (vertice)
            {
                case 0:
                    numeratore = edgeFunction(p1, p2, p);
                    break;
                case 1:
                    numeratore = edgeFunction(p2, p0, p);
                    break;
                case 2:
                    numeratore = edgeFunction(p0, p1, p);
                    break;
                default:
                    break;
            }

            return numeratore / (edgeFunction(p0, p1, p2));
        }
    }
}