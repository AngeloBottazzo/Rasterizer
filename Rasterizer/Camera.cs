using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace Rasterizer
{
    [Serializable]
    public class Camera : Componente
    {
        public Camera() : base("Camera")
        {
        }

        public double FOV { get; set; } = Math.PI / 2;
        public int Rx { get; set; } = 200;
        public int Ry { get; set; } = 150;


        public Matrix3D MatriceProspettiva(double z)
        {
            var m = new Matrix3D(
                1 / -z, 0, 0, 0,
                0, 1 / -z, 0, 0,
                0, 0, 1, 0,
                0, 0, 0, 1
                );
            return m;
        }

        public Matrix3D MatriceProiezione()
        {
            var m = new Matrix3D(
                1 / Math.Tan(FOV / 2.0) * Rx, 0, 0, 0,
                0, 1 / Math.Tan(FOV / 2.0) * Rx, 0, 0,
                0, 0, -1, 0,
                0, 0, 0, 1
                );
            return m;
        }

        //Le matrici della telecamera devono essere opposte

        public override Matrix3D MatriceTraslazione()
        {
            var t = Posizione;
            var m = new Matrix3D(
                1, 0, 0, 0,
                0, 1, 0, 0,
                0, 0, 1, 0,
                -t.X, -t.Y, -t.Z, 1
                );
            return m;
        }

        public override Matrix3D MatriceRotazioneX()
        {
            var t = Rotazione;
            var m = new Matrix3D(
                1, 0, 0, 0,
                0, Math.Cos(-t.X), Math.Sin(-t.X), 0,
                0, -Math.Sin(-t.X), Math.Cos(-t.X), 0,
                0, 0, 0, 1
                );
            return m;
        }

        public override Matrix3D MatriceRotazioneY()
        {
            var t = Rotazione;
            var m = new Matrix3D(
                Math.Cos(-t.Y), 0, -Math.Sin(-t.Y), 0,
                0, 1, 0, 0,
                Math.Sin(-t.Y), 0, Math.Cos(-t.Y), 0,
                0, 0, 0, 1
                );
            return m;
        }
        public override Matrix3D MatriceRotazioneZ()
        {
            var t = Rotazione;
            var m = new Matrix3D(
                Math.Cos(-t.Z), Math.Sin(-t.Z), 0, 0,
                -Math.Sin(-t.Z), Math.Cos(-t.Z), 0, 0,
                0, 0, 1, 0,
                0, 0, 0, 1
                );
            return m;
        }
    }
}
