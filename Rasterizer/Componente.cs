using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Xml.Serialization;

namespace Rasterizer
{
    [Serializable]
    public class Componente : Nodo
    {
        public string Nome { get; set; }
        public Vettore Posizione { get; set; } = new Vettore("Pos.");
        public Vettore Scala { get; set; } = new Vettore("Scala") { X = 1, Y = 1, Z = 1 };
        public Vettore Rotazione { get; set; } = new Vettore("Rot.");
        public ObservableCollection<Nodo> Figli { get; } = new ObservableCollection<Nodo>();
        public ObservableCollection<Nodo> Facce { get; } = new ObservableCollection<Nodo>();
        public ObservableCollection<Nodo> Vertici { get; } = new ObservableCollection<Nodo>();

        static int n = 0;

        public Componente(string nome = null)
        {
            Nome = nome??("Componente "+n++);
        }
        public override string ToString()
        {
            return Nome;
        }

        public void AggiungiFiglio(Nodo figlio)
        {
            Figli.Add(figlio);
            figlio.Padre = this;
        }
        public void AggiungiFaccia(Nodo faccia)
        {
            Facce.Add(faccia);
            faccia.Padre = this;
        }
        public void AggiungiVertice(Nodo vertice)
        {
            Vertici.Add(vertice);
            vertice.Padre = this;
        }

        public void TrasformaInSpazioGlobale()
        {
            //resetto i valori globali
            FaiSuTeETuttiIFigli((c)=>
            {
                foreach (Vettore vettore in c.Vertici)
                {
                    vettore.trasformato = vettore.relativo;
                }
            });

            //Trasformo tutti i vertici con le informazioni dei componenti
            FaiSuTeETuttiIFigli((c) =>
            {
                c.TrasformaInBaseAiGenitori(c);
            });
        }

        private void TrasformaInBaseAiGenitori(Componente chi)
        {
            TrasformaComponente(chi);
            Padre?.TrasformaInBaseAiGenitori(chi);
        }

        void TrasformaComponente(Componente daTrasformare)
        {
            daTrasformare.TrasformaSecondo(this);
        }

        public void TrasformaSecondo(Componente trasformante)
        {
            foreach (Vettore vertice in Vertici)
            {
                vertice.TrasformaSecondo(trasformante);
            }
        }

        public void FaiSuTeETuttiIFigli(Action<Componente> f)
        {
            f.Invoke(this);

            foreach (Componente figlio in Figli)
            {
                figlio.FaiSuTeETuttiIFigli(f);
            }
        }

        public void ProiettaVerticiConProspettiva(Camera camera)
        {
            foreach (Vettore vettore in Vertici)
            {
                vettore.Trasforma(camera.MatriceProspettiva(vettore.trasformato.Z));
                vettore.Trasforma(camera.MatriceProiezione());
            }
        }

        public virtual Matrix3D MatriceTraslazione()
        {
            var t = Posizione.relativo;
            var m = new Matrix3D(
                1, 0, 0, 0,
                0, 1, 0, 0,
                0, 0, 1, 0,
                t.X, t.Y, t.Z, 1
                );
            return m;
        }

        public virtual Matrix3D MatriceScala()
        {
            var t = Scala;
            var m = new Matrix3D(
                t.X, 0, 0, 0,
                0, t.Y, 0, 0,
                0, 0, t.Z, 0,
                0, 0, 0, 1
                );
            return m;
        }

        public virtual Matrix3D MatriceRotazioneX()
        {
            var t = Rotazione;
            var m = new Matrix3D(
                1, 0, 0, 0,
                0, Math.Cos(t.X), Math.Sin(t.X), 0,
                0, -Math.Sin(t.X), Math.Cos(t.X), 0,
                0, 0, 0, 1
                );
            return m;
        }

        public virtual Matrix3D MatriceRotazioneY()
        {
            var t = Rotazione;
            var m = new Matrix3D(
                Math.Cos(t.Y), 0, -Math.Sin(t.Y), 0,
                0, 1, 0, 0,
                Math.Sin(t.Y), 0, Math.Cos(t.Y), 0,
                0, 0, 0, 1
                );
            return m;
        }
        public virtual Matrix3D MatriceRotazioneZ()
        {
            var t = Rotazione;
            var m = new Matrix3D(
                Math.Cos(t.Z), Math.Sin(t.Z), 0, 0,
                -Math.Sin(t.Z), Math.Cos(t.Z), 0, 0,
                0, 0, 1, 0,
                0, 0, 0, 1
                );
            return m;
        }

        public Bitmap Renderizza(Camera camera, out double[,] zOut)
        {
            Bitmap bmp = new Bitmap((int)camera.Rx, (int)camera.Ry);
            
            double[,] zBuffer = new double[camera.Rx, camera.Ry];

            for (int i = 0; i < zBuffer.GetLength(0); i++)
                for (int j = 0; j < zBuffer.GetLength(1); j++)
                    zBuffer[i, j] = double.MaxValue;

            TrasformaInSpazioGlobale();

            FaiSuTeETuttiIFigli(c =>
            {
                c.TrasformaSecondo(camera);
                c.ProiettaVerticiConProspettiva(camera);
            });

            FaiSuTeETuttiIFigli(c =>
            {
                c.DisegnaFacce(bmp, zBuffer, camera);
            });

            zOut = zBuffer;

            return bmp;
        }

        private void DisegnaFacce(Bitmap bmp, double[,] zBuffer, Camera camera)
        {
            foreach (Faccia faccia in Facce)
            {
                faccia.Disegnati(bmp, zBuffer, camera);
            }
        }


        [OnDeserialized()]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            n++;
        }
    }

    [Serializable]
    public abstract class Nodo
    {
        public Componente Padre { get; set; }
    }

}
