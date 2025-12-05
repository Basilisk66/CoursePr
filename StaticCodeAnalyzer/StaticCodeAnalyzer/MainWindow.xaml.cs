using Microsoft.Win32;
using StaticCodeAnalyzer.Analyzers;
using StaticCodeAnalyzer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace StaticCodeAnalyzer
{
    public partial class MainWindow : Window
    {
        private CodeAnalyzer _currentAnalyzer;
        private List<AnalysisResult> _currentErrors;
        private string _currentFilePath = "";
        private string _selectedLanguage = "";
        private bool _isManualLanguageSelection = false; // Флаг ручного выбора языка

        public MainWindow()
        {
            InitializeComponent();
            _currentErrors = new List<AnalysisResult>();
            UpdateUIState();
            UpdateLineNumbers();

            // Подписываемся на события скролла для синхронизации
            CodeScrollViewer.ScrollChanged += CodeScrollViewer_ScrollChanged;
            LineNumbersScrollViewer.ScrollChanged += LineNumbersScrollViewer_ScrollChanged;
        }

        // Синхронизация скролла кода и нумерации строк
        private void CodeScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.VerticalChange != 0)
            {
                LineNumbersScrollViewer.ScrollToVerticalOffset(e.VerticalOffset);
            }
        }

        private void LineNumbersScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.VerticalChange != 0)
            {
                CodeScrollViewer.ScrollToVerticalOffset(e.VerticalOffset);
            }
        }

        // Обновление нумерации строк
        private void UpdateLineNumbers()
        {
            if (string.IsNullOrEmpty(CodeTextBox.Text))
            {
                LineNumbersControl.ItemsSource = new List<string> { "1" };
                return;
            }

            var lines = CodeTextBox.Text.Split('\n');
            var lineNumbers = new List<string>();
            for (int i = 1; i <= lines.Length; i++)
            {
                lineNumbers.Add(i.ToString());
            }
            LineNumbersControl.ItemsSource = lineNumbers;
        }

        // Обработчик изменения текста в текстовом поле
        private void CodeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateLineNumbers();
            UpdateUIState();

            // Автоматически определяем язык при вводе текста
            if (!string.IsNullOrWhiteSpace(CodeTextBox.Text))
            {
                // Сбрасываем флаг ручного выбора, если пользователь начал вводить новый код
                _isManualLanguageSelection = false;
                DetectLanguageFromText();
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

        // Метод для определения языка по содержимому текста
        private void DetectLanguageFromText()
        {
            string code = CodeTextBox.Text;

            // Если пользователь уже выбрал язык вручную, сохраняем его выбор
            if (_isManualLanguageSelection && !string.IsNullOrEmpty(_selectedLanguage))
            {
                UpdateLanguageDisplay();
                return;
            }

            // Используем улучшенную фабрику
            _currentAnalyzer = AnalyzerFactory.GetAnalyzer(code, _currentFilePath);

            if (_currentAnalyzer != null)
            {
                // Обновляем отображение языка с правильными названиями
                string languageName = GetLanguageDisplayName(_currentAnalyzer);
                LanguageTextBlock.Text = languageName;
                _selectedLanguage = languageName;
                _isManualLanguageSelection = false; // Автоматическое определение

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

        // Обновление отображения информации о языке
        private void UpdateLanguageDisplay()
        {
            if (_currentAnalyzer != null)
            {
                string languageName = GetLanguageDisplayName(_currentAnalyzer);
                LanguageTextBlock.Text = languageName;

                // Определяем что показывать в скобках
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

        // Обновление состояния UI элементов
        private void UpdateUIState()
        {
            bool hasCode = !string.IsNullOrWhiteSpace(CodeTextBox.Text);
            bool hasAnalyzer = _currentAnalyzer != null;

            // Кнопка выбора языка доступна только при наличии кода
            SelectLanguageButton.IsEnabled = hasCode;

            // Обновляем цвет кнопки выбора языка
            if (hasCode && hasAnalyzer)
            {
                SelectLanguageButton.Background = new SolidColorBrush(Color.FromRgb(0, 122, 204)); // Синий
                SelectLanguageButton.Foreground = Brushes.White;
            }
            else if (hasCode)
            {
                SelectLanguageButton.Background = new SolidColorBrush(Color.FromRgb(85, 85, 85)); // Серый
                SelectLanguageButton.Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 136));
            }
            else
            {
                SelectLanguageButton.Background = new SolidColorBrush(Color.FromRgb(85, 85, 85)); // Серый
                SelectLanguageButton.Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 136));
                SelectLanguageButton.IsEnabled = true;
            }

            // Обновляем кнопку Analyze Code
            if (hasCode && hasAnalyzer)
            {
                AnalyzeButton.Background = new SolidColorBrush(Color.FromRgb(104, 33, 122)); // Фиолетовый
                AnalyzeButton.Foreground = Brushes.White;
                AnalyzeButton.IsEnabled = true;
            }
            else
            {
                AnalyzeButton.Background = new SolidColorBrush(Color.FromRgb(85, 85, 85)); // Серый
                AnalyzeButton.Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 136));
                AnalyzeButton.IsEnabled = true;
            }

            // Обновляем кнопку Fix all issues - теперь только после анализа
            if (hasCode && _currentErrors != null && _currentErrors.Any())
            {
                ApplyAllButton.Background = new SolidColorBrush(Color.FromRgb(104, 33, 122)); // Фиолетовый
                ApplyAllButton.Foreground = Brushes.White;
                ApplyAllButton.IsEnabled = true;
            }
            else
            {
                ApplyAllButton.Background = new SolidColorBrush(Color.FromRgb(85, 85, 85)); // Серый
                ApplyAllButton.Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 136));
                ApplyAllButton.IsEnabled = true;
            }
        }

        // Загрузка файла
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

        // Анализ кода
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
                // Пытаемся определить язык автоматически, если он не выбран
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
                // Выполняем анализ через полиморфный вызов
                _currentErrors = _currentAnalyzer.Analyze(code);
                ErrorsDataGrid.ItemsSource = _currentErrors;

                // Показываем статистику
                ShowAnalysisStatistics();

                // Сбрасываем состояние кнопок исправлений
                ResetFixButtons();

                // Обновляем состояние UI после анализа
                UpdateUIState();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при анализе: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Показ статистики анализа
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

        // Обработчик выбора ошибки в списке
        private AnalysisResult _selectedError;
        private void ErrorsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedError = ErrorsDataGrid.SelectedItem as AnalysisResult;
            if (_selectedError != null)
            {
                // Показываем полное описание ошибки в Message
                ErrorMessageTextBlock.Text = _selectedError.Message ?? "No message available";

                // Обновляем кнопки
                UpdateFixButtons();
            }
            else
            {
                ErrorMessageTextBlock.Text = "Выберите ошибку для просмотра деталей";
                ResetFixButtons();
            }
        }

        // Обновление кнопок исправлений
        private void UpdateFixButtons()
        {
            if (_selectedError != null)
            {
                // Кнопка Show Possible Solution
                ShowFixButton.Background = new SolidColorBrush(Color.FromRgb(0, 122, 204)); // Синий
                ShowFixButton.Foreground = Brushes.White;
                ShowFixButton.IsEnabled = true;

                // Кнопка Fix
                bool canFix = !string.IsNullOrEmpty(_selectedError.FixedCode);
                if (canFix)
                {
                    ApplyFixButton.Background = new SolidColorBrush(Color.FromRgb(16, 124, 16)); // Зеленый
                    ApplyFixButton.Foreground = Brushes.White;
                    ApplyFixButton.IsEnabled = true;
                }
                else
                {
                    ApplyFixButton.Background = new SolidColorBrush(Color.FromRgb(85, 85, 85)); // Серый
                    ApplyFixButton.Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 136));
                    ApplyFixButton.IsEnabled = false;
                }
            }
        }

        // Сброс кнопок исправлений
        private void ResetFixButtons()
        {
            ShowFixButton.Background = new SolidColorBrush(Color.FromRgb(85, 85, 85)); // Серый
            ShowFixButton.Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 136));
            ShowFixButton.IsEnabled = false;

            ApplyFixButton.Background = new SolidColorBrush(Color.FromRgb(85, 85, 85)); // Серый
            ApplyFixButton.Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 136));
            ApplyFixButton.IsEnabled = false;

            FixPreviewGrid.Visibility = Visibility.Collapsed;
        }

        // Показать возможное решение
        private void ShowFixButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedError != null)
            {
                BeforeTextBox.Text = _selectedError.OriginalCode ?? "Исходный код не доступен";
                AfterTextBox.Text = string.IsNullOrEmpty(_selectedError.FixedCode)
                    ? "Автоматическое исправление недоступно"
                    : _selectedError.FixedCode;

                FixPreviewGrid.Visibility = Visibility.Visible;
            }
        }

        // Применить одно исправление
        private void ApplyFixButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedError != null && _currentAnalyzer != null && !string.IsNullOrEmpty(_selectedError.FixedCode))
            {
                try
                {
                    // Применяем исправление через полиморфный вызов
                    string fixedCode = _currentAnalyzer.FixCode(CodeTextBox.Text, new List<AnalysisResult> { _selectedError });
                    CodeTextBox.Text = fixedCode;

                    // Показываем сообщение об успехе
                    MessageBox.Show($"Строка {_selectedError.LineNumber} исправлена", "Успех",
                                  MessageBoxButton.OK, MessageBoxImage.Information);

                    // После применения исправления перезапускаем анализ
                    AnalyzeButton_Click(null, null);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при исправлении: {ex.Message}", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Применить все исправления
        private void ApplyAllButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentErrors != null && _currentAnalyzer != null && _currentErrors.Any())
            {
                try
                {
                    var fixableErrors = _currentErrors.Where(err => !string.IsNullOrEmpty(err.FixedCode)).ToList();

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
                        string fixedCode = _currentAnalyzer.FixCode(CodeTextBox.Text, fixableErrors);
                        CodeTextBox.Text = fixedCode;

                        MessageBox.Show($"Применено {fixableErrors.Count} исправлений", "Успех",
                                      MessageBoxButton.OK, MessageBoxImage.Information);

                        // Перезапускаем анализ
                        AnalyzeButton_Click(null, null);
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

        // Кнопка выбора языка
        private void SelectLanguageButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CodeTextBox.Text))
            {
                MessageBox.Show("Сначала введите код.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Создаем простое диалоговое окно для выбора языка
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

            // Список языков
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

            // Кнопка отмены
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

        // Метод для выбора языка из ручного выбора
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
                    // Если язык не распознан, пробуем автоматически определить
                    _selectedLanguage = "";
                    _isManualLanguageSelection = false;
                    DetectLanguageFromText();
                    return;
            }

            // Устанавливаем флаг ручного выбора
            _isManualLanguageSelection = true;

            // Обновляем отображение
            UpdateLanguageDisplay();
            UpdateUIState();
        }
    }
}