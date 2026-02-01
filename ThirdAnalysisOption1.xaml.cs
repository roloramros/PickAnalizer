using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Threading.Tasks;
using FloridaLotteryApp.Data;

namespace FloridaLotteryApp;

public partial class ThirdAnalysisOption1 : Window
{
    private ThirdAnalysisCardVM? _currentCard;
    private List<ThirdAnalysisCardVM> _matchedCards = new();
    private int _currentIndex = 0;

    public ThirdAnalysisOption1()
    {
        InitializeComponent();
        InitializeEventHandlers();
    }

    private void InitializeEventHandlers()
    {
        PreviousButton.Click += PreviousButton_Click;
        NextButton.Click += NextButton_Click;
    }

    public ThirdAnalysisOption1(ThirdAnalysisCardVM card)
    {
        InitializeComponent();
        _currentCard = card;

        SearchTab.IsSelected = true;
        InitializeEventHandlers();
        Loaded += ThirdAnalysisOption1_Loaded;

        LoadCard(card);
        
       
    }

    private async void ThirdAnalysisOption1_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= ThirdAnalysisOption1_Loaded;
        await LoadFilteredCodificacionesAsync();
    }

    private async Task LoadFilteredCodificacionesAsync()
    {
        try
        {
            ShowProgress(true, "Cargando codificaciones...");
            ResultsProgress.IsIndeterminate = true;

            var codificaciones = await Task.Run(
                () => DrawRepository.GetCodificacionesWithSingleCommonDigit());

            var guidePattern = BuildGuidePattern();
            if (guidePattern == null)
            {
                ResultsCounter.Text = "Resultados: 0 encontrados";
                return;
            }

            var guidePick3RepeatPattern = BuildPick3RepeatPattern(
                DigitsToString(_currentCard.Row1Pick3Digits),
                DigitsToString(_currentCard.Row3Pick3Digits));

            if (guidePick3RepeatPattern == null)
            {
                ResultsCounter.Text = "Resultados: 0 encontrados";
                return;
            }

            ResultsProgress.IsIndeterminate = false;
            ResultsProgress.Minimum = 0;
            ResultsProgress.Maximum = codificaciones.Count;
            ResultsProgress.Value = 0;
            ShowProgress(true, $"Procesando 0 de {codificaciones.Count}...");

            IProgress<int> progress = new System.Progress<int>(value =>
            {
                ResultsProgress.Value = value;
                ResultsProgressText.Text = $"Procesando {value} de {codificaciones.Count}...";
            });

            var matched = await Task.Run(() =>
            {
                var results = new List<ThirdAnalysisCardVM>();
                int processed = 0;

                foreach (var codificacion in codificaciones)
                {
                    var pairCard = BuildAnalysisPairCard(guidePattern, codificacion);
                    var guidePositions = GetRepeatedDigitPositions(
                        guidePattern.Pick3Value,
                        guidePattern.Pick4Value);
                    var resultPositions = GetRepeatedDigitPositions(
                        codificacion.Pick3,
                        codificacion.Pick4);

                    var cards = ThirdAnalysisCardVM.CreateMultipleFrom(
                        pairCard,
                        guidePositions,
                        resultPositions);

                    foreach (var card in cards)
                    {
                        if (MatchesGuideCounters(card) && MatchesPick3RepeatPattern(card, guidePick3RepeatPattern))
                        {
                            results.Add(card);
                        }
                    }

                    processed++;
                    if (processed % 10 == 0)
                    {
                        progress.Report(processed);
                    }
                }

                progress.Report(processed);
                return results;
            });

            _matchedCards = matched;
            ResultsCounter.Text = $"Resultados: {_matchedCards.Count} encontrados";

            if (_matchedCards.Count > 0)
            {
                DisplayMatchCard(_currentIndex);
            }

            UpdateNavigationButtons();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al cargar codificaciones: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
            _matchedCards = new List<ThirdAnalysisCardVM>();
        }
        finally
        {
            ShowProgress(false, string.Empty);
        }
    }

    private void ShowProgress(bool isVisible, string message)
    {
        var visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
        ResultsProgress.Visibility = visibility;
        ResultsProgressText.Visibility = visibility;
        ResultsProgressText.Text = message;
    }

    private void DisplayMatchCard(int index)
    {
        if (index < 0 || index >= _matchedCards.Count)
            return;

        var card = _matchedCards[index];
        LoadSearchCard(card);

        _currentIndex = index;
        ResultsCounter.Text = $"Resultado {_currentIndex + 1} de {_matchedCards.Count}";
    }

    private void UpdateNavigationButtons()
    {
        PreviousButton.IsEnabled = _currentIndex > 0;
        NextButton.IsEnabled = _currentIndex < _matchedCards.Count - 1;
    }

    private void PreviousButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentIndex > 0)
        {
            DisplayMatchCard(_currentIndex - 1);
            UpdateNavigationButtons();
        }
    }

    private void NextButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentIndex < _matchedCards.Count - 1)
        {
            DisplayMatchCard(_currentIndex + 1);
            UpdateNavigationButtons();
        }
    }

    private void LoadCard(ThirdAnalysisCardVM card)
    {
        try
        {
            // Cargar datos de la tarjeta
            Row1DateText.Text = card.Row1DateText;
            Row1DrawIcon.Text = card.Row1DrawIcon;
            Row1Pick3Items.ItemsSource = CopyDigitCollection(card.Row1Pick3Digits);
            Row1Pick4Items.ItemsSource = CopyDigitCollection(card.Row1Pick4Digits);
            Row1NextPick3Items.ItemsSource = CopyDigitCollection(card.Row1NextPick3Digits);
            Row1CodingItems.ItemsSource = CopyDigitCollection(card.Row1CodingDigits);
            
            Row2DateText.Text = card.Row2DateText;
            Row2DrawIcon.Text = card.Row2DrawIcon;
            Row2Pick3Items.ItemsSource = CopyDigitCollection(card.Row2Pick3Digits);
            Row2Pick4Items.ItemsSource = CopyDigitCollection(card.Row2Pick4Digits);
            Row2NextPick3Items.ItemsSource = CopyDigitCollection(card.Row2NextPick3Digits);
            Row2CodingItems.ItemsSource = CopyDigitCollection(card.Row2CodingDigits);
            
            Row3DateText.Text = card.Row3DateText;
            Row3DrawIcon.Text = card.Row3DrawIcon;
            Row3Pick3Items.ItemsSource = CopyDigitCollection(card.Row3Pick3Digits);
            Row3Pick4Items.ItemsSource = CopyDigitCollection(card.Row3Pick4Digits);
            Row3NextPick3Items.ItemsSource = CopyDigitCollection(card.Row3NextPick3Digits);
            Row3CodingItems.ItemsSource = CopyDigitCollection(card.Row3CodingDigits);
            
            Row4DateText.Text = card.Row4DateText;
            Row4DrawIcon.Text = card.Row4DrawIcon;
            Row4Pick3Items.ItemsSource = CopyDigitCollection(card.Row4Pick3Digits);
            Row4Pick4Items.ItemsSource = CopyDigitCollection(card.Row4Pick4Digits);
            Row4NextPick3Items.ItemsSource = CopyDigitCollection(card.Row4NextPick3Digits);
            Row4CodingItems.ItemsSource = CopyDigitCollection(card.Row4CodingDigits);
            
            // Cargar contadores
            Pick4MatchesRow1Row2.Text = card.Pick4MatchesRow1Row2 ?? "0C";
            CodingMatchesRow1Row2.Text = card.CodingMatchesRow1Row2 ?? "0C";
            Pick4MatchesRow3Row4.Text = card.Pick4MatchesRow3Row4 ?? "0C";
            CodingMatchesRow3Row4.Text = card.CodingMatchesRow3Row4 ?? "0C";
            

            // Esperar a que la UI se cargue completamente antes de dibujar las líneas
            this.Loaded += OnWindowLoaded;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al cargar la tarjeta: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LoadSearchCard(ThirdAnalysisCardVM card)
    {
        SearchRow1DateText.Text = card.Row1DateText;
        SearchRow1DrawIcon.Text = card.Row1DrawIcon;
        SearchRow1Pick3Items.ItemsSource = CopyDigitCollection(card.Row1Pick3Digits);
        SearchRow1Pick4Items.ItemsSource = CopyDigitCollection(card.Row1Pick4Digits);
        SearchRow1NextPick3Items.ItemsSource = CopyDigitCollection(card.Row1NextPick3Digits);
        SearchRow1CodingItems.ItemsSource = CopyDigitCollection(card.Row1CodingDigits);

        SearchRow2DateText.Text = card.Row2DateText;
        SearchRow2DrawIcon.Text = card.Row2DrawIcon;
        SearchRow2Pick3Items.ItemsSource = CopyDigitCollection(card.Row2Pick3Digits);
        SearchRow2Pick4Items.ItemsSource = CopyDigitCollection(card.Row2Pick4Digits);
        SearchRow2NextPick3Items.ItemsSource = CopyDigitCollection(card.Row2NextPick3Digits);
        SearchRow2CodingItems.ItemsSource = CopyDigitCollection(card.Row2CodingDigits);

        SearchRow3DateText.Text = card.Row3DateText;
        SearchRow3DrawIcon.Text = card.Row3DrawIcon;
        SearchRow3Pick3Items.ItemsSource = CopyDigitCollection(card.Row3Pick3Digits);
        SearchRow3Pick4Items.ItemsSource = CopyDigitCollection(card.Row3Pick4Digits);
        SearchRow3NextPick3Items.ItemsSource = CopyDigitCollection(card.Row3NextPick3Digits);
        SearchRow3CodingItems.ItemsSource = CopyDigitCollection(card.Row3CodingDigits);

        SearchRow4DateText.Text = card.Row4DateText;
        SearchRow4DrawIcon.Text = card.Row4DrawIcon;
        SearchRow4Pick3Items.ItemsSource = CopyDigitCollection(card.Row4Pick3Digits);
        SearchRow4Pick4Items.ItemsSource = CopyDigitCollection(card.Row4Pick4Digits);
        SearchRow4NextPick3Items.ItemsSource = CopyDigitCollection(card.Row4NextPick3Digits);
        SearchRow4CodingItems.ItemsSource = CopyDigitCollection(card.Row4CodingDigits);

        SearchPick4MatchesRow1Row2.Text = card.Pick4MatchesRow1Row2 ?? "0C";
        SearchCodingMatchesRow1Row2.Text = card.CodingMatchesRow1Row2 ?? "0C";
        SearchPick4MatchesRow3Row4.Text = card.Pick4MatchesRow3Row4 ?? "0C";
        SearchCodingMatchesRow3Row4.Text = card.CodingMatchesRow3Row4 ?? "0C";
        //SearchAnalysisSummary.Text = card.AnalysisSummary ?? string.Empty;
    }

    private GuidePattern? BuildGuidePattern()
    {
        if (_currentCard == null)
        {
            return null;
        }

        return new GuidePattern
        {
            Pick3Digits = CopyDigitCollection(_currentCard.Row1Pick3Digits),
            Pick4Digits = CopyDigitCollection(_currentCard.Row1Pick4Digits),
            NextPick3Digits = CopyDigitCollection(_currentCard.Row1NextPick3Digits),
            CodingDigits = CopyDigitCollection(_currentCard.Row1CodingDigits),
            DateText = _currentCard.Row1DateText,
            DrawIcon = _currentCard.Row1DrawIcon,
            Pick3Value = DigitsToString(_currentCard.Row1Pick3Digits),
            Pick4Value = DigitsToString(_currentCard.Row1Pick4Digits)
        };
    }

    private AnalysisPairCardVM BuildAnalysisPairCard(GuidePattern guide, FilteredCodificacion codificacion)
    {
        return new AnalysisPairCardVM
        {
            GuidePick3Value = guide.Pick3Value,
            GuidePick4Value = guide.Pick4Value,
            GuideNextPick3Value = DigitsToString(guide.NextPick3Digits),
            GuideCodingValue = DigitsToString(guide.CodingDigits),
            GuideDateText = guide.DateText,
            GuideDrawIcon = guide.DrawIcon,
            GuidePick3Digits = CopyDigitCollection(guide.Pick3Digits),
            GuidePick4Digits = CopyDigitCollection(guide.Pick4Digits),
            GuideNextPick3Digits = CopyDigitCollection(guide.NextPick3Digits),
            GuideCodingDigits = CopyDigitCollection(guide.CodingDigits),

            ResPick3Value = codificacion.Pick3,
            ResPick4Value = codificacion.Pick4,
            ResNextPick3Value = codificacion.NextPick3,
            ResDateText = codificacion.Date.ToString("yyyy-MM-dd"),
            ResDrawIcon = codificacion.DrawTime == "M" ? "☀️" : "🌙",
            ResPick3Digits = BuildDigitsFromNumber(codificacion.Pick3),
            ResPick4Digits = BuildDigitsFromNumber(codificacion.Pick4),
            ResNextPick3Digits = BuildDigitsFromNumber(codificacion.NextPick3),
            ResCodingDigits = BuildCodingDigits(codificacion.Pick3, codificacion.Pick4)
        };
    }

    private bool MatchesGuideCounters(ThirdAnalysisCardVM card)
    {
        if (_currentCard == null)
        {
            return false;
        }

        return NormalizeCounter(card.Pick4MatchesRow1Row2) == NormalizeCounter(_currentCard.Pick4MatchesRow1Row2)
            && NormalizeCounter(card.CodingMatchesRow1Row2) == NormalizeCounter(_currentCard.CodingMatchesRow1Row2)
            && NormalizeCounter(card.Pick4MatchesRow3Row4) == NormalizeCounter(_currentCard.Pick4MatchesRow3Row4)
            && NormalizeCounter(card.CodingMatchesRow3Row4) == NormalizeCounter(_currentCard.CodingMatchesRow3Row4);
    }

    private static string NormalizeCounter(string? value)
        => string.IsNullOrWhiteSpace(value) ? "0C" : value.Trim();

    private static ObservableCollection<DigitVM> BuildDigitsFromNumber(string number)
    {
        var digits = new ObservableCollection<DigitVM>();
        if (string.IsNullOrWhiteSpace(number))
        {
            return digits;
        }

        foreach (char digit in number)
        {
            digits.Add(new DigitVM
            {
                Value = digit.ToString(),
                Bg = CreateFrozenBrush(Colors.White)
            });
        }

        return digits;
    }

    private static ObservableCollection<DigitVM> BuildCodingDigits(string pick3, string pick4)
    {
        var allDigits = (pick3 + pick4)
            .Where(char.IsDigit)
            .Select(d => int.Parse(d.ToString()))
            .Distinct()
            .OrderBy(d => d)
            .Select(d => d.ToString())
            .ToList();

        while (allDigits.Count < 6)
        {
            allDigits.Add("");
        }

        return new ObservableCollection<DigitVM>(
            allDigits.Take(6)
                .Select(digit => new DigitVM
                {
                    Value = digit,
                    Bg = CreateFrozenBrush(Colors.White)
                }));
    }

    private static string DigitsToString(IEnumerable<DigitVM> digits)
        => string.Concat(digits.Select(d => d.Value));

    private static bool[]? BuildPick3RepeatPattern(string row1Pick3, string row3Pick3)
    {
        if (string.IsNullOrWhiteSpace(row1Pick3) || row1Pick3.Length != 3 ||
            string.IsNullOrWhiteSpace(row3Pick3) || row3Pick3.Length != 3)
        {
            return null;
        }

        var pattern = new bool[3];
        for (int i = 0; i < 3; i++)
        {
            char top = row1Pick3[i];
            char bottom = row3Pick3[i];

            if (!char.IsDigit(top) || !char.IsDigit(bottom))
            {
                return null;
            }

            pattern[i] = top == bottom;
        }

        return pattern;
    }

    private static bool MatchesPick3RepeatPattern(ThirdAnalysisCardVM card, bool[] guidePattern)
    {
        var cardPattern = BuildPick3RepeatPattern(
            DigitsToString(card.Row1Pick3Digits),
            DigitsToString(card.Row3Pick3Digits));

        if (cardPattern == null || guidePattern.Length != 3)
        {
            return false;
        }

        for (int i = 0; i < 3; i++)
        {
            if (cardPattern[i] != guidePattern[i])
            {
                return false;
            }
        }

        return true;
    }

    private static (char digit, int pick3Position, int pick4Position)? GetRepeatedDigitPositions(string pick3, string pick4)
    {
        if (string.IsNullOrWhiteSpace(pick3) || pick3.Length != 3 ||
            string.IsNullOrWhiteSpace(pick4) || pick4.Length != 4)
            return null;

        for (int i = 0; i < pick3.Length; i++)
        {
            char digit = pick3[i];
            int positionInPick4 = pick4.IndexOf(digit, StringComparison.Ordinal);

            if (positionInPick4 >= 0)
            {
                return (digit, i, positionInPick4);
            }
        }

        return null;
    }

    private sealed class GuidePattern
    {
        public ObservableCollection<DigitVM> Pick3Digits { get; init; } = new();
        public ObservableCollection<DigitVM> Pick4Digits { get; init; } = new();
        public ObservableCollection<DigitVM> NextPick3Digits { get; init; } = new();
        public ObservableCollection<DigitVM> CodingDigits { get; init; } = new();
        public string DateText { get; init; } = string.Empty;
        public string DrawIcon { get; init; } = string.Empty;
        public string Pick3Value { get; init; } = string.Empty;
        public string Pick4Value { get; init; } = string.Empty;
    }


    private void SetAllFixedPick3Values()
    {
        // Fijar valores para fila 1
        var row1Digits = new ObservableCollection<DigitVM>
        {
            new DigitVM { Value = "1", Bg = CreateFrozenBrush(Colors.White) },
            new DigitVM { Value = "2", Bg = CreateFrozenBrush(Colors.White) },
            new DigitVM { Value = "3", Bg = CreateFrozenBrush(Colors.White) }
        };
        SearchRow1Pick3Items.ItemsSource = row1Digits;

        // Fijar valores para fila 2
        var row2Digits = new ObservableCollection<DigitVM>
        {
            new DigitVM { Value = "4", Bg = CreateFrozenBrush(Colors.LightBlue) },
            new DigitVM { Value = "5", Bg = CreateFrozenBrush(Colors.LightBlue) },
            new DigitVM { Value = "6", Bg = CreateFrozenBrush(Colors.LightBlue) }
        };
        SearchRow2Pick3Items.ItemsSource = row2Digits;

        // Fijar valores para fila 3
        var row3Digits = new ObservableCollection<DigitVM>
        {
            new DigitVM { Value = "7", Bg = CreateFrozenBrush(Colors.LightPink) },
            new DigitVM { Value = "8", Bg = CreateFrozenBrush(Colors.LightPink) },
            new DigitVM { Value = "9", Bg = CreateFrozenBrush(Colors.LightPink) }
        };
        SearchRow3Pick3Items.ItemsSource = row3Digits;

        // Fijar valores para fila 4 (igual que fila 2)
        SearchRow4Pick3Items.ItemsSource = CopyDigitCollection(row2Digits);
    }
    private void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        // Esperar un poco más para asegurar que todos los controles estén renderizados
        Dispatcher.BeginInvoke(new Action(() => 
        {
            CreateConnectingLines();
        }), DispatcherPriority.ContextIdle);
    }

    private void CreateConnectingLines()
    {
        try
        {
            // Asegurarse de que los ItemsControls están completamente cargados
            ForceItemsControlUpdate(Row1Pick3Items);
            ForceItemsControlUpdate(Row2Pick3Items);
            ForceItemsControlUpdate(Row1NextPick3Items);
            ForceItemsControlUpdate(Row2NextPick3Items);
            ForceItemsControlUpdate(Row3Pick3Items);
            ForceItemsControlUpdate(Row4Pick3Items);
            ForceItemsControlUpdate(Row3NextPick3Items);
            ForceItemsControlUpdate(Row4NextPick3Items);
            ForceItemsControlUpdate(Row3Pick4Items);
            ForceItemsControlUpdate(Row4Pick4Items);

            // Conectar Pick3: Fila 1 con Fila 2
            ConnectPick3Digits(Row1Pick3Items, Row2Pick3Items, Pick3LinksCanvas);
            
            // Conectar NextPick3: Fila 1 con Fila 2
            ConnectPick3Digits(Row1NextPick3Items, Row2NextPick3Items, NextPick3LinksCanvas);
            
            // Conectar Pick3: Fila 3 con Fila 4
            ConnectPick3Digits(Row3Pick3Items, Row4Pick3Items, Pick3LinksCanvas);
            
            // Conectar NextPick3: Fila 3 con Fila 4
            ConnectPick3Digits(Row3NextPick3Items, Row4NextPick3Items, NextPick3LinksCanvas);
            
            // Conectar Pick3 de fila 3 con Pick4 de fila 4
            ConnectPick3ToPick4Digits(Row3Pick3Items, Row4Pick4Items, Pick3Row3ToPick4Row4LinksCanvas);
            
            // Conectar Pick3 de fila 4 con Pick4 de fila 3
            ConnectPick3ToPick4Digits(Row4Pick3Items, Row3Pick4Items, Pick3Row4ToPick4Row3LinksCanvas);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creando líneas: {ex.Message}");
        }
    }

    private void ForceItemsControlUpdate(ItemsControl itemsControl)
    {
        // Forzar la actualización del ItemsControl para que genere sus contenedores
        itemsControl.UpdateLayout();
        itemsControl.ApplyTemplate();
        
        // Si es un ItemsControl con ItemContainerGenerator, forzar la generación
        if (itemsControl.ItemContainerGenerator.Status != System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
        {
            itemsControl.ItemContainerGenerator.StatusChanged += (s, e) => 
            {
                if (itemsControl.ItemContainerGenerator.Status == System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
                {
                    // Los contenedores están listos
                }
            };
        }
    }

    private void ConnectPick3Digits(ItemsControl topItemsControl, ItemsControl bottomItemsControl, Canvas canvas)
    {
        if (topItemsControl == null || bottomItemsControl == null || canvas == null)
            return;

        try
        {
            // Obtener los contenedores de los dígitos
            var topDigits = GetDigitContainers(topItemsControl);
            var bottomDigits = GetDigitContainers(bottomItemsControl);

            if (topDigits.Count != 3 || bottomDigits.Count != 3)
            {
                Console.WriteLine($"Warning: No hay 3 dígitos. Top: {topDigits.Count}, Bottom: {bottomDigits.Count}");
                return;
            }

            // Limpiar canvas antes de dibujar
            canvas.Children.Clear();

            // Conectar dígitos que coincidan
            for (int i = 0; i < 3; i++)
            {
                var topDigit = topDigits[i];
                var topText = GetDigitText(topDigit);
                
                for (int j = 0; j < 3; j++)
                {
                    var bottomDigit = bottomDigits[j];
                    var bottomText = GetDigitText(bottomDigit);
                    
                    if (topText == bottomText)
                    {
                        // Dibujar línea conectando los dígitos
                        DrawConnectingLine(canvas, topDigit, bottomDigit);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error conectando Pick3 dígitos: {ex.Message}");
        }
    }

    private void ConnectPick3ToPick4Digits(ItemsControl pick3ItemsControl, ItemsControl pick4ItemsControl, Canvas canvas)
    {
        if (pick3ItemsControl == null || pick4ItemsControl == null || canvas == null)
            return;

        try
        {
            // Obtener los contenedores de los dígitos
            var pick3Digits = GetDigitContainers(pick3ItemsControl);
            var pick4Digits = GetDigitContainers(pick4ItemsControl);

            if (pick3Digits.Count != 3 || pick4Digits.Count != 4)
            {
                Console.WriteLine($"Warning: Pick3: {pick3Digits.Count} dígitos, Pick4: {pick4Digits.Count} dígitos");
                return;
            }

            // Limpiar canvas antes de dibujar
            canvas.Children.Clear();

            // Conectar dígitos que coincidan
            for (int i = 0; i < 3; i++)
            {
                var pick3Digit = pick3Digits[i];
                var pick3Text = GetDigitText(pick3Digit);
                
                for (int j = 0; j < 4; j++)
                {
                    var pick4Digit = pick4Digits[j];
                    var pick4Text = GetDigitText(pick4Digit);
                    
                    if (pick3Text == pick4Text)
                    {
                        // Dibujar línea conectando los dígitos
                        DrawConnectingLine(canvas, pick3Digit, pick4Digit);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error conectando Pick3 a Pick4: {ex.Message}");
        }
    }

    private List<Border> GetDigitContainers(ItemsControl itemsControl)
    {
        var containers = new List<Border>();
        
        try
        {
            // Asegurarse de que los contenedores están generados
            itemsControl.ApplyTemplate();
            
            // Buscar todos los Border en el árbol visual
            containers = FindVisualChildren<Border>(itemsControl)
                .Where(b => b.Name == "" || b.Name.StartsWith("Border")) // Filtrar borders
                .ToList();

            // Si no encontramos borders, intentar otra estrategia
            if (containers.Count == 0)
            {
                // Buscar ContentPresenters y luego sus hijos Border
                var contentPresenters = FindVisualChildren<ContentPresenter>(itemsControl);
                foreach (var cp in contentPresenters)
                {
                    var border = FindVisualChild<Border>(cp);
                    if (border != null)
                    {
                        containers.Add(border);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error obteniendo contenedores: {ex.Message}");
        }
        
        return containers;
    }

    private string GetDigitText(Border digitBorder)
    {
        try
        {
            var textBlock = FindVisualChild<TextBlock>(digitBorder);
            return textBlock?.Text ?? "";
        }
        catch
        {
            return "";
        }
    }

    private void DrawConnectingLine(Canvas canvas, Border element1, Border element2)
    {
        try
        {
            // Verificar que los elementos tengan dimensiones válidas
            if (element1.ActualWidth == 0 || element1.ActualHeight == 0 ||
                element2.ActualWidth == 0 || element2.ActualHeight == 0)
                return;

            // Calcular centros absolutos
            var center1 = element1.TranslatePoint(new Point(element1.ActualWidth / 2, element1.ActualHeight / 2), canvas);
            var center2 = element2.TranslatePoint(new Point(element2.ActualWidth / 2, element2.ActualHeight / 2), canvas);
            
            // Calcular vector dirección de element1 a element2
            double dx = center2.X - center1.X;
            double dy = center2.Y - center1.Y;
            
            // Calcular distancia entre centros
            double distance = Math.Sqrt(dx * dx + dy * dy);
            
            // Si están en la misma posición, no dibujar línea
            if (distance == 0) return;
            
            // Calcular radio de cada círculo (mitad del ancho, ya que son círculos)
            double radius1 = element1.ActualWidth / 2;
            double radius2 = element2.ActualWidth / 2;
            
            // Normalizar vector dirección
            double unitX = dx / distance;
            double unitY = dy / distance;
            
            // Calcular puntos en los bordes
            Point startPoint = new Point(
                center1.X + (unitX * radius1),
                center1.Y + (unitY * radius1)
            );
            
            Point endPoint = new Point(
                center2.X - (unitX * radius2),
                center2.Y - (unitY * radius2)
            );
            
            // Crear línea CONTINUA NEGRA
            var line = new Line
            {
                X1 = startPoint.X,
                Y1 = startPoint.Y,
                X2 = endPoint.X,
                Y2 = endPoint.Y,
                Stroke = Brushes.Black,
                StrokeThickness = 2
            };
            
            canvas.Children.Add(line);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error dibujando línea: {ex.Message}");
        }
    }

    private T FindVisualChild<T>(DependencyObject parent, string childName = null) where T : DependencyObject
    {
        if (parent == null) return null;

        try
        {
            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                
                // Buscar por nombre si se especifica
                if (child is FrameworkElement frameworkElement && 
                    !string.IsNullOrEmpty(childName) && 
                    frameworkElement.Name == childName)
                {
                    return child as T;
                }
                
                // Buscar por tipo
                if (child is T result && (string.IsNullOrEmpty(childName) || childName == result.GetValue(FrameworkElement.NameProperty) as string))
                {
                    return result;
                }
                
                // Buscar recursivamente
                var foundChild = FindVisualChild<T>(child, childName);
                if (foundChild != null) return foundChild;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en FindVisualChild: {ex.Message}");
        }
        
        return null;
    }

    private IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
    {
        var results = new List<T>();
        
        if (parent == null) return results;

        try
        {
            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T childOfType)
                {
                    results.Add(childOfType);
                }

                foreach (var descendant in FindVisualChildren<T>(child))
                {
                    results.Add(descendant);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en FindVisualChildren: {ex.Message}");
        }
        
        return results;
    }

    private ObservableCollection<DigitVM> CopyDigitCollection(ObservableCollection<DigitVM> source)
    {
        var copy = new ObservableCollection<DigitVM>();
        foreach (var digit in source)
        {
            Color color = Colors.White;
            
            // Verificar si el brush es SolidColorBrush y obtener su color
            if (digit.Bg is SolidColorBrush solidBrush)
            {
                color = solidBrush.Color;
            }
            
            copy.Add(new DigitVM
            {
                Value = digit.Value,
                Bg = CreateFrozenBrush(color)  // Usar el color original
            });
        }
        return copy;
    }

    private static SolidColorBrush CreateFrozenBrush(Color color)
    {
        var brush = new SolidColorBrush(color);
        if (brush.CanFreeze)
        {
            brush.Freeze();
        }
        return brush;
    }

    private void PositionAnalysisButton_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Implementar navegación al análisis de posición
        MessageBox.Show("Funcionalidad del análisis de posición pendiente.", "Información", 
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ThirdAnalysisButton_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Implementar navegación al tercer análisis
        MessageBox.Show("Funcionalidad del tercer análisis pendiente.", "Información", 
            MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
