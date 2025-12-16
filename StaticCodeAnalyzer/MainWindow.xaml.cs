using Microsoft.Win32;
using StaticCodeAnalyzer.Analyzers;
using StaticCodeAnalyzer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace StaticCodeAnalyzer
{
    public partial class MainWindow : Window
    {
        private CodeAnalyzer _currentAnalyzer;
        private List<AnalysisResult> _currentErrors;
        private string _currentFilePath = "";
        private string _selectedLanguage = "";
        private bool _isManualLanguageSelection = false;
        private bool _isScrolling = false;
        private AnalysisResult _selectedError;
        private bool _isApplyingFix = false; // Flag to prevent language detection during fixes

        public MainWindow()
        {
            InitializeComponent(); // ← только ОДИН вызов!
            _currentErrors = new List<AnalysisResult>();
            UpdateUIState();
            UpdateLineNumbers();

            CodeScrollViewer.ScrollChanged += CodeScrollViewer_ScrollChanged;
            LineNumbersScrollViewer.ScrollChanged += LineNumbersScrollViewer_ScrollChanged;
            CodeTextBox.FontSize = 14;
        }

        private void CodeScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (_isScrolling) return;
            _isScrolling = true;
            try
            {
                LineNumbersScrollViewer.ScrollToVerticalOffset(e.VerticalOffset);
            }
            finally
            {
                _isScrolling = false;
            }
        }

        private void LineNumbersScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (_isScrolling) return;
            _isScrolling = true;
            try
            {
                CodeScrollViewer.ScrollToVerticalOffset(e.VerticalOffset);
            }
            finally
            {
                _isScrolling = false;
            }
        }

        private void UpdateLineNumbers()
        {
            string text = CodeTextBox.Text;

            // Handle empty text - show no line numbers
            if (string.IsNullOrEmpty(text))
            {
                LineNumbersControl.ItemsSource = null;
                return;
            }

            var lines = text.Split('\n');
            // Only show line numbers if there's actual content
            if (lines.Length == 1 && string.IsNullOrWhiteSpace(lines[0]))
            {
                LineNumbersControl.ItemsSource = null;
                return;
            }

            var lineNumbers = Enumerable.Range(1, lines.Length).Select(i => i.ToString()).ToList();
            LineNumbersControl.ItemsSource = lineNumbers;
        }

        private void CodeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateLineNumbers();
            UpdateUIState();

            // Don't detect language when applying fixes - preserve current analyzer
            if (_isApplyingFix)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(CodeTextBox.Text))
            {
                // Only auto-detect if not manually selected and analyzer is null
                if (!_isManualLanguageSelection && _currentAnalyzer == null)
                {
                    DetectLanguageFromText();
                }
            }
            else
            {
                LanguageTextBlock.Text = "Код не обнаружен";
                FileExtensionTextBlock.Text = "";
                _currentAnalyzer = null;
                _selectedLanguage = "";
                _isManualLanguageSelection = false;
                UpdateUIState();
            }
        }

        private void DetectLanguageFromText()
        {
            string code = CodeTextBox.Text;

            if (_isManualLanguageSelection && !string.IsNullOrEmpty(_selectedLanguage))
            {
                UpdateLanguageDisplay();
                return;
            }

            _currentAnalyzer = AnalyzerFactory.GetAnalyzer(code, _currentFilePath);

            if (_currentAnalyzer != null)
            {
                string languageName = GetLanguageDisplayName(_currentAnalyzer);
                LanguageTextBlock.Text = languageName;
                _selectedLanguage = languageName;
                _isManualLanguageSelection = false;
                UpdateLanguageDisplay();
            }
            else
            {
                LanguageTextBlock.Text = "Неизвестный язык";
                FileExtensionTextBlock.Text = "(не удалось определить)";
                _selectedLanguage = "";
                _isManualLanguageSelection = false;
            }

            UpdateUIState();
        }

        private void UpdateLanguageDisplay()
        {
            if (_currentAnalyzer != null)
            {
                string languageName = GetLanguageDisplayName(_currentAnalyzer);
                LanguageTextBlock.Text = languageName;

                if (!string.IsNullOrEmpty(_currentFilePath))
                {
                    string fileName = System.IO.Path.GetFileName(_currentFilePath);
                    FileExtensionTextBlock.Text = $"(из файла: {fileName})";
                }
                else if (_isManualLanguageSelection)
                {
                    FileExtensionTextBlock.Text = "(определено вручную)";
                }
                else
                {
                    FileExtensionTextBlock.Text = "(определено автоматически)";
                }
            }
        }

        private string GetLanguageDisplayName(CodeAnalyzer analyzer)
        {
            var analyzerType = analyzer.GetType().Name;

            if (analyzerType == "CSharpAnalyzer")
                return "C#";
            else if (analyzerType == "CppAnalyzer")
                return "C++";
            else if (analyzerType == "JavaAnalyzer")
                return "Java";
            else if (analyzerType == "PythonAnalyzer")
                return "Python";
            else if (analyzerType == "JavaScriptAnalyzer")
                return "JavaScript";
            else if (analyzerType == "HtmlAnalyzer")
                return "HTML";
            else
                return analyzerType.Replace("Analyzer", "");
        }

        private void UpdateUIState()
        {
            bool hasCode = !string.IsNullOrWhiteSpace(CodeTextBox.Text);
            bool hasAnalyzer = _currentAnalyzer != null;

            SelectLanguageButton.IsEnabled = hasCode;

            if (hasCode && hasAnalyzer)
            {
                SelectLanguageButton.Background = new SolidColorBrush(Color.FromRgb(0, 122, 204));
                SelectLanguageButton.Foreground = Brushes.White;
            }
            else if (hasCode)
            {
                SelectLanguageButton.Background = new SolidColorBrush(Color.FromRgb(85, 85, 85));
                SelectLanguageButton.Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 136));
            }
            else
            {
                SelectLanguageButton.Background = new SolidColorBrush(Color.FromRgb(85, 85, 85));
                SelectLanguageButton.Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 136));
                SelectLanguageButton.IsEnabled = true;
            }

            if (hasCode && hasAnalyzer)
            {
                AnalyzeButton.Background = new SolidColorBrush(Color.FromRgb(104, 33, 122));
                AnalyzeButton.Foreground = Brushes.White;
                AnalyzeButton.IsEnabled = true;
            }
            else
            {
                AnalyzeButton.Background = new SolidColorBrush(Color.FromRgb(85, 85, 85));
                AnalyzeButton.Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 136));
                AnalyzeButton.IsEnabled = true;
            }

            if (hasCode && _currentErrors != null && _currentErrors.Any())
            {
                ApplyAllButton.Background = new SolidColorBrush(Color.FromRgb(104, 33, 122));
                ApplyAllButton.Foreground = Brushes.White;
                ApplyAllButton.IsEnabled = true;
            }
            else
            {
                ApplyAllButton.Background = new SolidColorBrush(Color.FromRgb(85, 85, 85));
                ApplyAllButton.Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 136));
                ApplyAllButton.IsEnabled = true;
            }
        }

        private void LoadFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "All supported files (*.cs, *.html, *.htm, *.java, *.cpp, *.h, *.py, *.js, *.txt)|*.cs;*.html;*.htm;*.java;*.cpp;*.h;*.py;*.js;*.txt|C# files (*.cs)|*.cs|HTML files (*.html, *.htm)|*.html;*.htm|Java files (*.java)|*.java|C++ files (*.cpp, *.h)|*.cpp;*.h|Python files (*.py)|*.py|JavaScript files (*.js)|*.js|Text files (*.txt)|*.txt|All files (*.*)|*.*";

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    _currentFilePath = openFileDialog.FileName;
                    string code = System.IO.File.ReadAllText(_currentFilePath);
                    CodeTextBox.Text = code;

                    DetectLanguageFromText();
                    UpdateUIState();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SelectLanguageButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CodeTextBox.Text))
            {
                MessageBox.Show("Сначала введите код.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var languageDialog = new Window
            {
                Title = "Выберите язык программирования",
                Width = 300,
                Height = 400,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
                Foreground = Brushes.White
            };

            var stackPanel = new StackPanel();

            var languages = new List<string> { "C#", "C++", "Java", "Python", "JavaScript", "HTML" };

            foreach (var language in languages)
            {
                var button = new Button
                {
                    Content = language,
                    Margin = new Thickness(10),
                    Padding = new Thickness(10),
                    Background = new SolidColorBrush(Color.FromRgb(62, 62, 66)),
                    Foreground = Brushes.White
                };

                button.Click += (s, args) =>
                {
                    _selectedLanguage = language;
                    SelectLanguageFromChoice();
                    languageDialog.Close();
                };

                stackPanel.Children.Add(button);
            }

            var cancelButton = new Button
            {
                Content = "Отмена",
                Margin = new Thickness(10),
                Padding = new Thickness(10),
                Background = new SolidColorBrush(Color.FromRgb(85, 85, 85)),
                Foreground = Brushes.White
            };

            cancelButton.Click += (s, args) => languageDialog.Close();
            stackPanel.Children.Add(cancelButton);

            languageDialog.Content = stackPanel;
            languageDialog.ShowDialog();
        }

        private void SelectLanguageFromChoice()
        {
            if (string.IsNullOrEmpty(_selectedLanguage) || string.IsNullOrWhiteSpace(CodeTextBox.Text))
                return;

            switch (_selectedLanguage)
            {
                case "C#":
                    _currentAnalyzer = new CSharpAnalyzer();
                    break;
                case "C++":
                    _currentAnalyzer = new CppAnalyzer();
                    break;
                case "Java":
                    _currentAnalyzer = new JavaAnalyzer();
                    break;
                case "Python":
                    _currentAnalyzer = new PythonAnalyzer();
                    break;
                case "JavaScript":
                    _currentAnalyzer = new JavaScriptAnalyzer();
                    break;
                case "HTML":
                    _currentAnalyzer = new HtmlAnalyzer();
                    break;
                default:
                    _selectedLanguage = "";
                    _isManualLanguageSelection = false;
                    DetectLanguageFromText();
                    return;
            }

            _isManualLanguageSelection = true;
            UpdateLanguageDisplay();
            UpdateUIState();
        }

        private void AnalyzeButton_Click(object sender, RoutedEventArgs e)
        {
            string code = CodeTextBox.Text;

            if (string.IsNullOrWhiteSpace(code))
            {
                MessageBox.Show("Введите код или загрузите файл.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_currentAnalyzer == null)
            {
                DetectLanguageFromText();

                if (_currentAnalyzer == null)
                {
                    MessageBox.Show("Не удалось определить язык. Пожалуйста, выберите язык вручную.", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            try
            {
                _currentErrors = _currentAnalyzer
                    .Analyze(code)
                    .OrderBy(error => error.LineNumber)  // Изменено: e -> error
                    .ToList();
                ErrorsDataGrid.ItemsSource = _currentErrors;
                ShowAnalysisStatistics();
                ResetFixButtons();
                UpdateUIState();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при анализе: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ReAnalyze()
        {
            if (string.IsNullOrWhiteSpace(CodeTextBox.Text) || _currentAnalyzer == null) return;

            try
            {
                _currentErrors = _currentAnalyzer
                    .Analyze(CodeTextBox.Text)
                    .OrderBy(e => e.LineNumber)
                    .ToList();
                ErrorsDataGrid.ItemsSource = _currentErrors;
                ShowAnalysisStatistics();
                ResetFixButtons();
                UpdateUIState();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при анализе: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowAnalysisStatistics()
        {
            int errorCount = _currentErrors.Count;
            int warningCount = _currentErrors.Count(e => e.ErrorType == "Warning");
            int infoCount = _currentErrors.Count(e => e.ErrorType == "Info");

            string stats = $"Найдено: {errorCount} проблем";
            if (warningCount > 0) stats += $", {warningCount} предупреждений";
            if (infoCount > 0) stats += $", {infoCount} информационных";

            LanguageTextBlock.Text = LanguageTextBlock.Text.Split('-')[0].Trim() + $" - {stats}";
        }

        private void ErrorsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedError = ErrorsDataGrid.SelectedItem as AnalysisResult;
            if (_selectedError != null)
            {
                ErrorMessageTextBlock.Text = _selectedError.Message ?? "No message available";
                UpdateFixButtons();
            }
            else
            {
                ErrorMessageTextBlock.Text = "Выберите ошибку для просмотра деталей";
                ResetFixButtons();
            }
        }

        private void UpdateFixButtons()
        {
            if (_selectedError != null)
            {
                ShowFixButton.Background = new SolidColorBrush(Color.FromRgb(0, 122, 204));
                ShowFixButton.Foreground = Brushes.White;
                ShowFixButton.IsEnabled = true;

                bool canFix = _selectedError.FixedCode != null;
                if (canFix)
                {
                    ApplyFixButton.Background = new SolidColorBrush(Color.FromRgb(16, 124, 16));
                    ApplyFixButton.Foreground = Brushes.White;
                    ApplyFixButton.IsEnabled = true;
                }
                else
                {
                    ApplyFixButton.Background = new SolidColorBrush(Color.FromRgb(85, 85, 85));
                    ApplyFixButton.Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 136));
                    ApplyFixButton.IsEnabled = false;
                }
            }
        }

        private void ResetFixButtons()
        {
            ShowFixButton.Background = new SolidColorBrush(Color.FromRgb(85, 85, 85));
            ShowFixButton.Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 136));
            ShowFixButton.IsEnabled = false;

            ApplyFixButton.Background = new SolidColorBrush(Color.FromRgb(85, 85, 85));
            ApplyFixButton.Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 136));
            ApplyFixButton.IsEnabled = false;

            FixPreviewGrid.Visibility = Visibility.Collapsed;
        }

        private void ShowFixButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedError != null)
            {
                BeforeTextBox.Text = _selectedError.OriginalCode ?? "Исходный код не доступен";

                if (_selectedError.FixedCode != null)
                {
                    if (_selectedError.FixedCode == "")
                    {
                        AfterTextBox.Text = "[пустая строка - будет удалена]";
                    }
                    else
                    {
                        AfterTextBox.Text = _selectedError.FixedCode;
                    }
                }
                else
                {
                    AfterTextBox.Text = "Автоматическое исправление недоступно";
                }

                FixPreviewGrid.Visibility = Visibility.Visible;
            }
        }

        private void ApplyFixButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedError != null && _currentAnalyzer != null && _selectedError.FixedCode != null)
            {
                try
                {
                    _isApplyingFix = true; // Prevent language detection during fix
                    string fixedCode = _currentAnalyzer.FixCode(CodeTextBox.Text,
                        new List<AnalysisResult> { _selectedError });
                    CodeTextBox.Text = fixedCode;
                    _isApplyingFix = false; // Re-enable language detection

                    MessageBox.Show($"Строка {_selectedError.LineNumber} исправлена", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    ReAnalyze(); // ← сохраняет текущий _currentAnalyzer!
                }
                catch (Exception ex)
                {
                    _isApplyingFix = false; // Ensure flag is reset on error
                    MessageBox.Show($"Ошибка при исправлении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ApplyAllButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentErrors != null && _currentAnalyzer != null && _currentErrors.Any())
            {
                try
                {
                    var fixableErrors = _currentErrors.Where(err => err.FixedCode != null).ToList();

                    if (!fixableErrors.Any())
                    {
                        MessageBox.Show("Автоматически исправляемых ошибок не найдено.", "Информация",
                                      MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    var result = MessageBox.Show($"Применить все {fixableErrors.Count} исправления?",
                                               "Применить все исправления",
                                               MessageBoxButton.YesNo,
                                               MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        ApplyAllFixes(fixableErrors);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при применении исправлений: {ex.Message}", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Исправляемых ошибок не найдено.", "Информация",
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        private void ApplyAllFixes(List<AnalysisResult> fixableErrors)
        {
            if (_currentAnalyzer == null)
            {
                MessageBox.Show("Анализатор не выбран", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                _isApplyingFix = true; // Prevent language detection during fixes

                // Use the analyzer's FixCode method which handles line number updates correctly
                string fixedCode = _currentAnalyzer.FixCode(CodeTextBox.Text, fixableErrors);

                // Count fixes: if code changed, assume fixes were applied
                // The analyzer's FixCode method processes errors correctly from end to start
                int appliedFixes = 0;

                if (CodeTextBox.Text != fixedCode)
                {
                    // Code was modified, so fixes were applied
                    // We can't easily count exact fixes due to line number shifts,
                    // but we know the analyzer processed all fixable errors
                    appliedFixes = fixableErrors.Count;
                }

                CodeTextBox.Text = fixedCode;
                _isApplyingFix = false; // Re-enable language detection

                MessageBox.Show($"Применено {appliedFixes} из {fixableErrors.Count} исправлений", "Успех",
                              MessageBoxButton.OK, MessageBoxImage.Information);

                // Повторный анализ для обновления UI
                ReAnalyze();
            }
            catch (Exception ex)
            {
                _isApplyingFix = false; // Ensure flag is reset on error
                MessageBox.Show($"Ошибка при применении исправлений: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}