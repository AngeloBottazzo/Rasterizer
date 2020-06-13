using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Xml.Serialization;

namespace Rasterizer
{
    [Serializable]
    public class Vettore : Nodo
    {
        static int nNome = 0;
        public string Nome { get; }

        public Point3D relativo = new Point3D();
        public Point3D trasformato;

        public double X
        {
            get => relativo.X;
            set => relativo.X = value;
        }
        public double Y
        {
            get => relativo.Y;
            set => relativo.Y = value;
        }
        public double Z
        {
            get => relativo.Z;
            set => relativo.Z = value;
        }

        public double x
        {
            get => trasformato.X;
        }
        public double y
        {
            get => trasformato.Y;
        }
        public double z
        {
            get => trasformato.Z;
        }


        public static string StringaLettereDaNumero(int n)
        {
            return (n/26>0?StringaLettereDaNumero(n/26-1):"")+char.ConvertFromUtf32(n % 26 + 65);
        }

        public Vettore(string nome = null)
        {
            Nome = nome??StringaLettereDaNumero(nNome++);
        }
        public void TrasformaSecondo(Componente componente)
        {
            if(componente is Camera) //se il componente per la trasformazione è la telecamera, serve eseguire le trasformazioni in ordine inverso
            {
                Trasforma(componente.MatriceTraslazione());
                Trasforma(componente.MatriceRotazioneZ());
                Trasforma(componente.MatriceRotazioneY());
                Trasforma(componente.MatriceRotazioneX());
            }
            else
            {
                Trasforma(componente.MatriceScala());
                Trasforma(componente.MatriceRotazioneX());
                Trasforma(componente.MatriceRotazioneY());
                Trasforma(componente.MatriceRotazioneZ());
                Trasforma(componente.MatriceTraslazione());
            }
        }

        public void Trasforma(Matrix3D matriceTrasformazione)
        {
            trasformato = Point3D.Multiply(trasformato, matriceTrasformazione);
        }

        [OnDeserialized()]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            nNome++;
        }
        /*
public static Vector3D Moltiplica(Matrix3D mt, Vector3D v)
{
   double[,] m1 = {
       { mt.M11, mt.M12, mt.M13, mt.M14 },
       { mt.M21, mt.M22, mt.M23, mt.M24 },
       { mt.M31, mt.M32, mt.M33, mt.M34 },
       { mt.OffsetX, mt.OffsetY, mt.OffsetZ, mt.M44 }
   };

   double[,] m2 = {
       { v.X },
       { v.Y },
       { v.Z },
       { 1 },
   };

   double[,] m3 = new double[m1.GetLength(0), m2.GetLength(1)];

   for (int i = 0; i < m1.GetLength(0); i++)
   {
       for (int j = 0; j < m2.GetLength(1); j++)
       {
           double somma = 0;

           for (int n = 0; n < m1.GetLength(1); n++)
           {
               somma += m1[i, n] * m2[n, j];
           }

           m3[i, j] = somma;
       }
   }

   return new Vector3D(m3[0,0], m3[1,0], m3[2,0]);
}
*/
    }
}
