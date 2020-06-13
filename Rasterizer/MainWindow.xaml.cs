using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using System.Xml.Serialization;

namespace Rasterizer
{
    /// <summary>
    /// Logica di interazione per MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public Componente Mondo { get; private set; }
        public MainWindow()
        {
            InitializeComponent();

            Mondo = new Componente();
            Mondo.AggiungiFiglio(new Camera());
            
            /*
            Componente tetraedro = new Componente("Tetraedro");
            Mondo.AggiungiFiglio(tetraedro);
            
            Mondo = new Componente("ciao");
            Mondo.Figli.Add(new Componente("ciao"));
            Mondo.Figli.Add(new Componente ("ciao"));
            Mondo.Figli.Add(new Componente ("ciao"));
            Mondo.Figli[0].Figli.Add(new Componente ("ciao"));
            Mondo.Figli[0].Figli.Add(new Componente ("ciao"));
            Mondo.Figli[0].Figli.Add(new Componente ("ciao"));*/
            
            Albero.ItemsSource = Mondo.Figli;
        }

        private void BtnEliminaNodo_Click(object sender, RoutedEventArgs e)
        {
            Nodo questo = ((sender as FrameworkElement).DataContext as Nodo);

            switch (questo.GetType().Name)
            {
                case "Camera":
                    goto case "Componente";
                case "Componente":
                    questo.Padre.Figli.Remove(questo);
                    break;
                case "Vettore":
                    questo.Padre.Vertici.Remove(questo);
                    break;
                case "Faccia":
                    questo.Padre.Facce.Remove(questo);
                    break;
                default:
                    break;
            }
        }

        private void BtnCreaComponente_Click(object sender, RoutedEventArgs e)
        {
            Mondo.AggiungiFiglio(new Componente());
        }

        private void BtnAggiungiComponente_Click(object sender, RoutedEventArgs e)
        {
            ((sender as FrameworkElement).DataContext as Componente).AggiungiFiglio(new Componente());
        }

        private void BtnCreaCamera_Click(object sender, RoutedEventArgs e)
        {
            Mondo.AggiungiFiglio(new Camera());
        }

        private void BtnCreaFaccia_Click(object sender, RoutedEventArgs e)
        {
            (Albero.SelectedItem as Componente)?.AggiungiFaccia(new Faccia());
        }

        private void BtnCreaVertice_Click(object sender, RoutedEventArgs e)
        {
            (Albero.SelectedItem as Componente)?.AggiungiVertice(new Vettore());
        }

        private void BtnRenderizza_Click(object sender, ExecutedRoutedEventArgs e)
        {
            var camera = (Albero.SelectedItem as Camera) ?? (Camera)Mondo.Figli.FirstOrDefault(c => c is Camera);
            if (camera == null)
            {
                MessageBox.Show("Inserisci almeno una telecamera");
                return;
            }

            //renderizzo colori

            var bmp = Mondo.Renderizza(camera, out double[,] zbuffer);

            MemoryStream memoryStream = new MemoryStream();
            bmp.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Bmp);
            memoryStream.Position = 0;
            BitmapImage immagine = new BitmapImage();
            immagine.BeginInit();
            immagine.StreamSource = memoryStream;
            immagine.EndInit();
            ImageView.Source = immagine;

            double zMin = double.MaxValue;
            double zMax = double.MinValue;

            //creo immagine z-buffer

            for (int i = 0; i < zbuffer.GetLength(0); i++)
            {
                for (int j = 0; j < zbuffer.GetLength(1); j++)
                {
                    if (zMin > zbuffer[i, j])
                        zMin = zbuffer[i, j];

                    if (zbuffer[i, j] != double.MaxValue && zMax < zbuffer[i, j])
                        zMax = zbuffer[i, j];
                }
            }

            Bitmap zmap = new Bitmap(zbuffer.GetLength(0), zbuffer.GetLength(1));
            for (int i = 0; i < zbuffer.GetLength(0); i++)
            {
                for (int j = 0; j < zbuffer.GetLength(1); j++)
                {
                    if (zbuffer[i, j] != double.MaxValue)
                    {
                        int l = 255 - Math.Max(0, Math.Min((int)((zbuffer[i, j] - zMin) / (zMax - zMin) * 205), 255));
                        zmap.SetPixel(i, j, System.Drawing.Color.FromArgb(255, l, l, l));
                    }
                }
            }

            memoryStream = new MemoryStream();
            zmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Bmp);
            memoryStream.Position = 0;
            BitmapImage zimg = new BitmapImage();
            zimg.BeginInit();
            zimg.StreamSource = memoryStream;
            zimg.EndInit();
            ZBufferView.Source = zimg;


            //Creo misto
            Bitmap fmap = new Bitmap(camera.Rx, camera.Ry);
            for (int i = 0; i < camera.Rx; i++)
            {
                for (int j = 0; j < camera.Ry; j++)
                {
                    if (zbuffer[i, j] != double.MaxValue)
                    {
                        fmap.SetPixel(i, j, Color.FromArgb(255, bmp.GetPixel(i, j).R * zmap.GetPixel(i, j).R / 255, bmp.GetPixel(i, j).G * zmap.GetPixel(i, j).G / 255, bmp.GetPixel(i, j).B * zmap.GetPixel(i, j).B / 255));
                    }
                }
            }

            memoryStream = new MemoryStream();
            fmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Bmp);
            memoryStream.Position = 0;
            BitmapImage fimg = new BitmapImage();
            fimg.BeginInit();
            fimg.StreamSource = memoryStream;
            fimg.EndInit();
            FusioneView.Source = fimg;
        }

        private void BtnSalvaScena_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog fileDialog = new SaveFileDialog
            {
                Filter = "File scena (*.scene)|*.scene|Tutti i file (*.*)|*.*"
            };

            if (fileDialog.ShowDialog() ?? false)
            {
                IFormatter formatter = new BinaryFormatter();
                Stream stream = new FileStream(fileDialog.FileName, FileMode.Create, FileAccess.Write, FileShare.None);
                formatter.Serialize(stream, Mondo);
                stream.Close();
            }
        }

        private void BtnCaricaScena_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog
            {
                Filter = "File scena (*.scene)|*.scene|Tutti i file (*.*)|*.*"
            };

            if (fileDialog.ShowDialog() ?? false)
            {
                FileStream fs = new FileStream(fileDialog.FileName, FileMode.Open);
                try
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    Mondo = (Componente)formatter.Deserialize(fs);
                    Albero.ItemsSource = Mondo.Figli;
                }
                catch (Exception er)
                {
                    MessageBox.Show("Apertura non riuscita: " + er.Message);
                }
                finally
                {
                    fs.Close();
                }

            }

        }

        private void BtnSalvaComponente_Click(object sender, RoutedEventArgs e)
        {
            Componente daSalvare = (Albero.SelectedItem as Componente);

            if (daSalvare == null)
                return;

            SaveFileDialog fileDialog = new SaveFileDialog
            {
                Filter = "File componente (*.comp)|*.comp|Tutti i file (*.*)|*.*"
            };

            if (fileDialog.ShowDialog() ?? false)
            {
                IFormatter formatter = new BinaryFormatter();
                Stream stream = new FileStream(fileDialog.FileName, FileMode.Create, FileAccess.Write, FileShare.None);
                formatter.Serialize(stream, daSalvare);
                stream.Close();
            }
        }

        private void BtnCaricaComponente_Click(object sender, RoutedEventArgs e)
        {

            Componente inCuiCaricare = (Albero.SelectedItem as Componente) ?? Mondo;


            OpenFileDialog fileDialog = new OpenFileDialog
            {
                Filter = "File componente (*.comp)|*.comp|Tutti i file (*.*)|*.*"
            };

            if (fileDialog.ShowDialog() ?? false)
            {
                FileStream fs = new FileStream(fileDialog.FileName, FileMode.Open);
                try
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    Componente c = (Componente)formatter.Deserialize(fs);
                    if (inCuiCaricare != Mondo && MessageBox.Show("Inserire nel componente selezionato? In caso negativo, sarà messo alla radice.", "Inserimento componente", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                        inCuiCaricare.AggiungiFiglio(c);
                    else
                        Mondo.AggiungiFiglio(c);

                }
                catch (Exception er)
                {
                    MessageBox.Show("Apertura non riuscita: " + er.Message);
                }
                finally
                {
                    fs.Close();
                }
            }
        }

        private void BtnSalvaImmagine_Click(object sender, RoutedEventArgs e)
        {
            PngBitmapEncoder encoder = new PngBitmapEncoder();

            switch (Tabbe.SelectedIndex)
            {
                default:
                    encoder.Frames.Add(BitmapFrame.Create((ImageView.Source as BitmapImage)));
                    break;
                case 1:
                    encoder.Frames.Add(BitmapFrame.Create((ZBufferView.Source as BitmapImage)));
                    break;
                case 2:
                    encoder.Frames.Add(BitmapFrame.Create((FusioneView.Source as BitmapImage)));
                    break;
            }


            SaveFileDialog fileDialog = new SaveFileDialog
            {
                Filter = "File PNG (*.png)|*.png"
            };

            if (fileDialog.ShowDialog() ?? false)
            {

                using (var fileStream = new System.IO.FileStream(fileDialog.FileName, System.IO.FileMode.Create))
                {
                    encoder.Save(fileStream);
                }
            }
        }
    }

    public class GradiGradienti : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (double)value / (2 * Math.PI) * 360;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                value = (double)0;

            return (double)value / 360 * (2 * Math.PI);
        }
    }
}
