using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms.Integration;
using Path = System.IO.Path;
using System.IO;
using System.Drawing;

using SharpNeat.View;
using SharpNeat.Domains;
using SharpNeat.Genomes.Neat;
using System.Xml;
using StarcraftNN.OrganismInterfaces;

namespace UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string genomesDir = Path.Combine(Environment.GetEnvironmentVariable("USERPROFILE"), "sc_populations");

        private WindowsFormsHost _host;
        private NeatGenomeView _view;

        public MainWindow()
        {
            InitializeComponent();

            _view = new NeatGenomeView();
            _view.BackColor = System.Drawing.Color.White;

            _host = new WindowsFormsHost();
            _host.Child = _view;
            grid.Children.Add(_host);
            Grid.SetRow(_host, 1);
        }

        protected NeatGenome getGenome(string filename)
        {
            XmlDocument document = new XmlDocument();
            var name = Path.GetFileNameWithoutExtension(filename);
            var iface = createOrganismInterface(name);
            var factory = iface.CreateGenomeFactory();
            document.Load(filename);
            var genomes = NeatGenomeXmlIO.LoadCompleteGenomeList(document, true, factory);
            var genome = genomes.OrderByDescending(x => x.EvaluationInfo.Fitness).First();
            return genome;
        }

        IOrganismInterface createOrganismInterface(string typename)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var assembly = AppDomain.CurrentDomain.GetAssemblies().Where(x => Path.GetFileNameWithoutExtension(x.Location) == "StarcraftNN").Single();
            var type = assembly.GetType("StarcraftNN.OrganismInterfaces." + typename);
            if (type == null)
                throw new Exception("Genome file doesn't match any defined organism interfaces.");
            var iface = (IOrganismInterface)Activator.CreateInstance(type);
            return iface;
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            // Configure save file dialog box
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = "network"; // Default file name
            dlg.DefaultExt = ".png"; // Default file extension
            dlg.Filter = "Image Files (*.png)|*.png"; // Filter files by extension 

            // Process save file dialog box results 
            if (dlg.ShowDialog() == true)
            {
                string filename = dlg.FileName;
                var encoder = new PngBitmapEncoder();
                var control = _host.Child;
                Bitmap b = new Bitmap(control.Width, control.Height);
                control.DrawToBitmap(b, new System.Drawing.Rectangle(0, 0, b.Width, b.Height));
                b.Save(filename);
            }
        }

        private void btnLoad_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();

            openFileDialog.Multiselect = false;
            openFileDialog.InitialDirectory = genomesDir;
            openFileDialog.Filter = "Genome Files (*.xml)|*.xml|All Files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                var genome = getGenome(openFileDialog.FileName);
                _view.RefreshView(genome);
            }
        }
    }
}
