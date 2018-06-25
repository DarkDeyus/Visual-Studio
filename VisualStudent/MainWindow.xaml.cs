using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using Microsoft.Win32;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.Reflection;
using PluginContracts;
using Microsoft.Build.Execution;
using Microsoft.Build.BuildEngine;
using System.Collections.ObjectModel;

namespace VisualStudent
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        bool wasChanged = true;
        string pathtoproj = "";
        public MainWindow()
        {
            InitializeComponent();
            
            string path = System.AppDomain.CurrentDomain.BaseDirectory;
            string[] dllFileNames = null;

            if (Directory.Exists(path))
            {
                dllFileNames = Directory.GetFiles(path, "*.dll");
            }

            //read all dlls and add them to the assembly collection
            ICollection<Assembly> assemblies = new List<Assembly>(dllFileNames.Length);
            foreach (string dllFile in dllFileNames)
            {
                if (!dllFile.Contains("PluginContracts.dll"))
                {
                    AssemblyName an = AssemblyName.GetAssemblyName(dllFile);
                    Assembly assembly = Assembly.Load(an);
                    assemblies.Add(assembly);
                }
            }

            //add classes from dlls
            Type pluginType = typeof(IPlugin);
            ICollection<Type> pluginTypes = new List<Type>();
            foreach (Assembly assembly in assemblies)
            {
                if (assembly != null)
                {
                    Type[] types = assembly.GetTypes();
                    foreach (Type type in types)
                    {
                        if (type.IsInterface || type.IsAbstract)
                        {
                            continue;
                        }
                        else
                        {
                            if (type.GetInterface(pluginType.FullName) != null)
                            {
                                pluginTypes.Add(type);
                            }
                        }
                    }
                }
            }

            //activate all classes and store them in the list of IPlugins
            ICollection<IPlugin> plugins = new List<IPlugin>(pluginTypes.Count);
            foreach (Type type in pluginTypes)
            {
                IPlugin plugin = (IPlugin)Activator.CreateInstance(type);
                plugins.Add(plugin);
            }

            //Create menu items and add them to the menu header
            foreach(IPlugin plugin in plugins)
            {
                MenuItem item = new MenuItem();
                item.Header = plugin.Name;
                item.IsCheckable = true;
                item.Tag = plugin;
                item.Click += Plugin_Click;
                ListOfPlugins.Items.Add(item);
            }

        }

        private void Exit_Click(object sender, RoutedEventArgs e) => Close();

        private void OpenProject_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            String path = "";
            bool containsCsproj = false;
            if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                DirectoryInfo folder = new DirectoryInfo(folderBrowserDialog.SelectedPath);

                foreach (FileInfo file in folder.GetFiles())
                {
                    if (file.Extension == ".csproj")
                    {
                        containsCsproj = true;
                        path = file.FullName;
                        pathtoproj = file.FullName;
                        break;
                    }
                }

                if (!containsCsproj)
                {
                    MessageBox.Show("This folder does not contains a C# project!", "An error occured", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            else
                return;

            //root
            TreeViewItem Parent = new TreeViewItem
            {
                Tag = path,
                Header = "Project: " + System.IO.Path.GetFileName(folderBrowserDialog.SelectedPath)
            };

            ProjectTree.Items.Add(Parent);
            AddFiles(Parent, folderBrowserDialog.SelectedPath);
            Parent.MouseDoubleClick += ChooseProject;

            //recursive fuction to add all files to the tree
            void AddFiles(TreeViewItem parent, string pathToParent)
            {
                DirectoryInfo folder = new DirectoryInfo(pathToParent);

                //dir
                foreach (DirectoryInfo dir in folder.GetDirectories())
                {
                    TreeViewItem curdir = new TreeViewItem();
                    curdir.Header = System.IO.Path.GetFileName(dir.FullName);
                    parent.Items.Add(curdir);

                    AddFiles(curdir, dir.FullName);
                }

                //files
                foreach (FileInfo file in folder.GetFiles())
                {
                    if (file.Extension == ".cs")
                    {
                        TreeViewItem newChild = new TreeViewItem();
                        newChild.Header = System.IO.Path.GetFileName(file.FullName);
                        parent.Items.Add(newChild);

                        newChild.Tag = file.FullName;
                        newChild.MouseDoubleClick += OpenFileFromTree_MouseDoubleClick;
                    }
                }

            }
        }

        private void OpenFileFromTree_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is TreeViewItem)
            {
                if (!((TreeViewItem)sender).IsSelected)
                {
                    return;
                }
            }

            string fileloc = (string)(((TreeViewItem)sender).Tag);

            TabItem file;
            RichTextBox txt;

            (file, txt) = CreateTabItem(System.IO.Path.GetFileName(fileloc));                     

            txt.Document.Blocks.Clear();
            txt.Document.Blocks.Add(new Paragraph(new Run(File.ReadAllText(fileloc))));
            file.Tag = fileloc;

            TabControl.Items.Add(file);
            TabControl.SelectedIndex = TabControl.Items.Count - 1;
        }

        private void ChooseProject(object sender, MouseButtonEventArgs e) => pathtoproj = (string)((TreeViewItem)sender).Tag;       

        private void Save_As_Click(object sender, RoutedEventArgs e)
        {
            if (TabControl.SelectedIndex == -1)
                return;
            
            TabItem item = (TabItem)(TabControl.Items[TabControl.SelectedIndex]);
            StackPanel stack = (StackPanel)item.Header;
            TextBlock block = (TextBlock)stack.Children[0];
            RichTextBox textblock = (RichTextBox)item.Content;

            string filename = block.Text;
            int starLocation = filename.IndexOf('*');
            if (filename != "" && starLocation != -1)
            {
                filename = filename.Remove(starLocation, 2);
            }

    
            filename += ".cs";
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "C# Files|*.cs";
            saveFileDialog.Title = "Select a C# file";
            saveFileDialog.FileName = filename;

            if (saveFileDialog.ShowDialog() == true)
            {
                block.Text = filename.Remove((filename.Length - 3), 3);
                File.WriteAllText(saveFileDialog.FileName, (new TextRange(textblock.Document.ContentStart, textblock.Document.ContentEnd).Text));
                item.Tag = saveFileDialog.FileName;
            }                                  
            
        }

        private void SaveCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (TabControl.SelectedIndex == -1)
                return;
            
            TabItem item = (TabItem)(TabControl.Items[TabControl.SelectedIndex]);
            StackPanel stack = (StackPanel)item.Header;
            TextBlock block = (TextBlock)stack.Children[0];
            RichTextBox textblock = (RichTextBox)item.Content;
            
            //no file associated
            if(item.Tag == null)
            {
                Save_As_Click(sender, e);
                return;
            }

            string filename = block.Text;
            int starLocation = filename.IndexOf('*');
            if (filename != "" && starLocation != -1)
            {
                filename = filename.Remove(starLocation, 2);
            }

            File.WriteAllText((string)item.Tag, (new TextRange(textblock.Document.ContentStart, textblock.Document.ContentEnd).Text));
            block.Text = filename;
        }

        private void OpenFileCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "C# Files|*.cs";
            openFileDialog.Title = "Select a C# file";

            if (openFileDialog.ShowDialog() == true)
            {
                TabItem file = new TabItem();
                RichTextBox txt = new RichTextBox();

                file.Header = System.IO.Path.GetFileName(openFileDialog.FileName);
                file.Content = txt;

                txt.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
                txt.Document.Blocks.Clear();
                txt.Document.Blocks.Add(new Paragraph(new Run(File.ReadAllText(openFileDialog.FileName))));

                TabControl.Items.Add(file);
                TabControl.SelectedIndex = TabControl.Items.Count - 1;
            }
        }

        private void NewFileCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            TabItem file;
            RichTextBox textbox;
            (file, textbox) = CreateTabItem("New File");           

            TabControl.Items.Add(file);
            TabControl.SelectedIndex = TabControl.Items.Count - 1;
           
        }

        private void About_Click(object sender, RoutedEventArgs e) => MessageBox.Show("This is a simple C# editor and compiler.", "About", MessageBoxButton.OK, MessageBoxImage.Information);

        private void ExecuteCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            CompileProgram(pathtoproj);
            if (OptionComboBox.SelectedIndex == 1 && ((ObservableCollection<Error>)(ErrorList.DataContext)).Count == 0) RunApp();

        }

        (TabItem, RichTextBox) CreateTabItem(string headerText)
        {
            TabItem tabitem = new TabItem();
            RichTextBox txt = new RichTextBox();

            Button button = new Button();
            button.Content = "X";
            button.Width = 15;
            button.FontWeight = FontWeights.Bold;
            button.Click += CloseTab;

            TextBlock textBlock = new TextBlock();
            textBlock.Text = headerText + " ";

            StackPanel stack = new StackPanel();
            stack.Orientation = Orientation.Horizontal;

            stack.Children.Add(textBlock);
            stack.Children.Add(button);

            tabitem.Header = stack;
            tabitem.Content = txt;

            txt.TextChanged += RichTextBox_TextChanged;
            txt.PreviewKeyUp += Txt_PreviewKeyDown;
            txt.Tag = tabitem;
           
            button.Tag = tabitem;
            txt.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;

            return (tabitem, txt);
        }

        private void Txt_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            RichTextBox box = (RichTextBox)sender;            
            RunPlugins(box);                       
        }

        //add a * to the edited text
        private void RichTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {          
            if(wasChanged)
            {
                TabItem tab = (TabItem)((RichTextBox)sender).Tag;
                StackPanel stack = (StackPanel)tab.Header;
                TextBlock block = (TextBlock)stack.Children[0];

                if (!block.Text.Contains('*'))
                    block.Text += "* ";
            }
                         
        }

        private void CloseTab(object sender, RoutedEventArgs e)
        {
            TabItem item = (TabItem)((Button)sender).Tag;
            StackPanel stack = (StackPanel)item.Header;
            TextBlock block = (TextBlock)stack.Children[0];

            if(block.Text.Contains('*'))
            {
                if (MessageBox.Show("Do you want to close unsaved document?", "Close document", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                    return;
            }
            TabControl.Items.Remove(item);
            
        }

        private void Plugin_Click(object sender, RoutedEventArgs e)
        {            
            foreach (TabItem item in TabControl.Items  )
            {
                RichTextBox box = (RichTextBox)item.Content;
                RunPlugins(box);
            }           
        }

        ObservableCollection<Error> Convert(ObservableCollection<BuildErrorEventArgs> collection)
        {
            ObservableCollection<Error> result = new ObservableCollection<Error>();
            foreach (var item in collection)
            {
                string part1 = "error " + item.Code;
                string part2 = item.Message;
                string part3 = item.File + "(" + item.LineNumber + "," + item.ColumnNumber + ")";
                result.Add(new Error(part1, part2, part3));
            }
            return result;
        }

        void CompileProgram(string path)
        {
            string log = "";
            void LogMessage(string message) => log += message;

            if (path == "")
                return;           

            BuildManager manager = BuildManager.DefaultBuildManager;
            ConsoleLogger consoleLogger = new ConsoleLogger(LoggerVerbosity.Normal, LogMessage, null, null);
            ErrorLogger errorLogger = new ErrorLogger(); 
            ProjectInstance projectInstance = new ProjectInstance(path);
            BuildParameters parameters = new BuildParameters();
            parameters.DetailedSummary = true;
            parameters.Loggers = new List<ILogger>() { consoleLogger, errorLogger };
            var result = manager.Build(parameters, new BuildRequestData(projectInstance, new string[] { "Build" }));
            
            var buildResult = result.ResultsByTarget["Build"];
            var buildResultItems = buildResult.Items;

            Output.Text = log;
            ErrorList.DataContext = Convert(errorLogger.errors);
        }

        void RunApp()
        {
            
            string path = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(pathtoproj), "bin\\debug");
            DirectoryInfo directoryInfo = new DirectoryInfo(path);
            var tab = directoryInfo.GetFiles("*.exe");
            System.Diagnostics.Process.Start(tab[0].FullName);
        }

        void RunPlugins(RichTextBox box)
        {
            wasChanged = false;
            
            FlowDocument flowDocument = box.Document;
            TextRange textRange = new TextRange(flowDocument.ContentStart, flowDocument.ContentEnd);
            textRange.ClearAllProperties();

            foreach (MenuItem plugin in ListOfPlugins.Items)
            {
                if (plugin.IsChecked)
                {
                    ((IPlugin)plugin.Tag).Do(box);
                   
                }
            }
            wasChanged = true;
        }
    }

}
