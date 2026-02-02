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

            if (_currentCard == null)
            {
                ResultsCounter.Text = "Resultados: 0 encontrados";
                return;
            }

            // ======= EXTRAER EL GUIDEPAIR DEL CURRENT_CARD =======
            string row3Full = DigitsToString(_currentCard.Row3Pick3Digits) + DigitsToString(_currentCard.Row3Pick4Digits) + DigitsToString(_currentCard.Row3NextPick3Digits);
            string row1Full = DigitsToString(_currentCard.Row1Pick3Digits) + DigitsToString(_currentCard.Row1Pick4Digits) + DigitsToString(_currentCard.Row1NextPick3Digits);
            var guidePair = (row3Full, row1Full); // (tercera fila, primera fila)
            

            // ======= CREAR LISTA DE TODOS LOS FULLNUMBERS =======
            // Extraer FullNumber de cada codificación
            var allFullNumbers = codificaciones
                .Select(c => c.FullNumber)
                .Where(fn => !string.IsNullOrWhiteSpace(fn) && fn.Length == 10)
                .Distinct()
                .ToList();
            
            MessageBox.Show($"Total de FullNumbers disponibles: {allFullNumbers.Count}", 
                        "Depuración", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
            // ======= FIN CREACIÓN LISTA =======

            // ======= PROCESAR CADA FULLNUMBER =======
            var allResults = new List<(string Original, string Compatible)>();
            
            int processedCount = 0;
            foreach (var fullNumber in allFullNumbers)
            {
                // Aplicar la función a cada FullNumber
                var resultsForThisNumber = FindFilteredPairs(
                    fullNumberOriginal: fullNumber,
                    allFullNumbers: allFullNumbers,
                    guidePair: guidePair);
                // Acumular resultados
                allResults.AddRange(resultsForThisNumber);
                processedCount++;
                //if (processedCount == 1)
                //{
                //    break;
                //}
                
            }

            var validCuartetos = FormCuartetosFromPairs(allResults);

            string mensaje = $"Cuartetos válidos encontrados: {validCuartetos.Count}";
            MessageBox.Show(mensaje, "Resultados", MessageBoxButton.OK, MessageBoxImage.Information);

            string pick4MatchesRow1Row2 = _currentCard?.Pick4MatchesRow1Row2 ?? "0C";
            string codingMatchesRow1Row2 = _currentCard?.CodingMatchesRow1Row2 ?? "0C";
            string pick4MatchesRow3Row4 = _currentCard?.Pick4MatchesRow3Row4 ?? "0C";
            string codingMatchesRow3Row4 = _currentCard?.CodingMatchesRow3Row4 ?? "0C";

            // 2. Filtrar cuartetos por contadores
            var cuartetosFiltrados = FilterCuartetosByCounters(
                cuartetos: validCuartetos,
                refPick4MatchesRow1Row2: pick4MatchesRow1Row2,
                refCodingMatchesRow1Row2: codingMatchesRow1Row2,
                refPick4MatchesRow3Row4: pick4MatchesRow3Row4,
                refCodingMatchesRow3Row4: codingMatchesRow3Row4);

            // 3. Mostrar resultados
            if (cuartetosFiltrados.Count > 0)
        {
            string resultadoMensaje  = $"TOTAL: {cuartetosFiltrados.Count} cuartetos\n\n";
            
            int mostrar = Math.Min(cuartetosFiltrados.Count, 10);
            resultadoMensaje  += $"Primeros {mostrar}:\n\n";
            
            for (int i = 0; i < mostrar; i++)
            {
                var c = cuartetosFiltrados[i];
                resultadoMensaje  += $"{i+1}. {c.First} | {c.Second} | {c.Third} | {c.Fourth}\n";
            }
            
            if (cuartetosFiltrados.Count > 10)
            {
                resultadoMensaje  += $"\n... y {cuartetosFiltrados.Count - 10} más";
            }
            
            MessageBox.Show(resultadoMensaje , "Resultados", MessageBoxButton.OK, MessageBoxImage.Information);
        }



            // ======= FIN PROCESAMIENTO =======








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

            _matchedCards = new List<ThirdAnalysisCardVM>();
            _currentIndex = 0;

            IProgress<int> progress = new System.Progress<int>(value =>
            {
                ResultsProgress.Value = value;
                ResultsProgressText.Text = $"Procesando {value} de {codificaciones.Count}...";
            });

            IProgress<ThirdAnalysisCardVM> matchProgress = new System.Progress<ThirdAnalysisCardVM>(card =>
            {
                _matchedCards.Add(card);

                if (_matchedCards.Count == 1)
                {
                    DisplayMatchCard(0);
                }
                else
                {
                    ResultsCounter.Text = $"Resultado {_currentIndex + 1} de {_matchedCards.Count}";
                }

                UpdateNavigationButtons();
            });

            await Task.Run(() =>
            {
                int processed = 0;

                foreach (var codificacion in codificaciones)
                {
                    if (TryBuildFirstAnalysisGuide(codificacion, out var guide, out var rows))
                    {
                        foreach (var row in rows)
                        {
                            var pairCard = AnalysisPairCardVM.Create(guide, row);
                            var guidePositions = GetRepeatedDigitPositions(
                                pairCard.GuidePick3Value,
                                pairCard.GuidePick4Value);
                            var resultPositions = GetRepeatedDigitPositions(
                                pairCard.ResPick3Value,
                                pairCard.ResPick4Value);

                            var cards = ThirdAnalysisCardVM.CreateMultipleFrom(
                                pairCard,
                                guidePositions,
                                resultPositions);

                            foreach (var card in cards)
                            {
                                if (MatchesGuideCounters(card) && MatchesPick3RepeatPattern(card, guidePick3RepeatPattern))
                                {
                                    matchProgress.Report(card);
                                }
                            }
                        }
                    }

                    processed++;
                    if (processed % 1 == 0)
                    {
                        progress.Report(processed);
                    }
                }

                progress.Report(processed);
            });

            if (_matchedCards.Count == 0)
            {
                ResultsCounter.Text = "Resultados: 0 encontrados";
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

    public List<(string Original, string Compatible)> FindFilteredPairs(string fullNumberOriginal, List<string> allFullNumbers, (string First, string Second) guidePair)
    {
        var result = new List<(string, string)>();
        
        // Validación básica
        if (string.IsNullOrWhiteSpace(fullNumberOriginal) || fullNumberOriginal.Length != 10)
            return result;
        
        if (allFullNumbers == null || allFullNumbers.Count == 0)
            return result;
        
        // 1. Extraer y analizar el fullNumberOriginal
        string originalP3 = fullNumberOriginal.Substring(0, 3);
        string originalP4 = fullNumberOriginal.Substring(3, 4);
        
        // Encontrar dígito repetido y posiciones en el original
        char? originalRepeatedDigit = null;
        int originalPosP3 = -1;
        int originalPosP4 = -1;
        
        for (int i = 0; i < 3; i++)
        {
            char digit = originalP3[i];
            int posInP4 = originalP4.IndexOf(digit);
            if (posInP4 >= 0)
            {
                originalRepeatedDigit = digit;
                originalPosP3 = i;
                originalPosP4 = posInP4;
                break;
            }
        }
        
        // Si el original no tiene dígito repetido, retornar vacío
        if (!originalRepeatedDigit.HasValue)
            return result;
        
        // 2. Analizar el patrón de la guidePair
        bool[] guidePattern = new bool[3];
        string guideP3_1 = guidePair.First.Substring(0, 3);
        string guideP3_2 = guidePair.Second.Substring(0, 3);
        
        for (int i = 0; i < 3; i++)
        {
            guidePattern[i] = (guideP3_1[i] == guideP3_2[i]);
        }
        
        // 3. Procesar cada candidato en allFullNumbers
        foreach (string candidate in allFullNumbers)
        {
            // Saltar si es el mismo número original
            if (candidate == fullNumberOriginal)
                continue;
            
            // Validar que tenga 10 dígitos
            if (string.IsNullOrWhiteSpace(candidate) || candidate.Length != 10)
                continue;
            
            // 3.1 Extraer Pick3 y Pick4 del candidato
            string candidateP3 = candidate.Substring(0, 3);
            string candidateP4 = candidate.Substring(3, 4);
            
            // 3.2 Encontrar dígito repetido en el candidato
            char? candidateRepeatedDigit = null;
            int candidatePosP3 = -1;
            int candidatePosP4 = -1;
            
            for (int i = 0; i < 3; i++)
            {
                char digit = candidateP3[i];
                int posInP4 = candidateP4.IndexOf(digit);
                if (posInP4 >= 0)
                {
                    candidateRepeatedDigit = digit;
                    candidatePosP3 = i;
                    candidatePosP4 = posInP4;
                    break;
                }
            }
            
            // Si el candidato no tiene dígito repetido, saltar
            if (!candidateRepeatedDigit.HasValue)
                continue;
            
            // 3.3 Primer filtro: mismas posiciones del dígito repetido
            if (originalPosP3 != candidatePosP3 || originalPosP4 != candidatePosP4)
                continue;
            
            // 3.4 Segundo filtro: mismo patrón que guidePair
            bool candidatePatternValid = true;
            
            // Comparar las posiciones de los Pick3 del par (original, candidato)
            for (int i = 0; i < 3; i++)
            {
                bool positionsAreEqual = (originalP3[i] == candidateP3[i]);
                
                // El patrón debe coincidir exactamente
                if (guidePattern[i] != positionsAreEqual)
                {
                    candidatePatternValid = false;
                    break;
                }
            }
            
            // 3.5 Si pasa ambos filtros, agregar a resultados
            if (candidatePatternValid)
            {
                result.Add((fullNumberOriginal, candidate));
            }
        }
        
        return result;
    }

    public List<(string First, string Second, string Third, string Fourth)> FormCuartetosFromPairs(List<(string Original, string Compatible)> allPairs)
    {
        var allValidCuartetos = new List<(string, string, string, string)>();
        
        // 1. Agrupar todas las parejas por su Original
        var groupsByOriginal = new Dictionary<string, List<string>>();
        
        foreach (var pair in allPairs)
        {
            if (!groupsByOriginal.ContainsKey(pair.Original))
            {
                groupsByOriginal[pair.Original] = new List<string>();
            }
            
            // Evitar duplicados dentro del mismo grupo
            if (!groupsByOriginal[pair.Original].Contains(pair.Compatible))
            {
                groupsByOriginal[pair.Original].Add(pair.Compatible);
            }
        }
        
        // 2. Procesar cada grupo
        foreach (var group in groupsByOriginal)
        {
            string original = group.Key;        // Primero
            List<string> compatibles = group.Value;
            
            // Necesitamos al menos 2 compatibles para formar cuartetos
            if (compatibles.Count >= 2)
            {
                // 3. Generar todas las combinaciones de 2 compatibles
                for (int i = 0; i < compatibles.Count - 1; i++)
                {
                    for (int j = i + 1; j < compatibles.Count; j++)
                    {
                        string compatible1 = compatibles[i];  // Segundo
                        string compatible2 = compatibles[j];  // Tercero
                        
                        // 4. Extraer los Pick3 de cada FullNumber (primeros 3 dígitos)
                        string pick3_1 = original.Substring(0, 3);
                        string pick3_2 = compatible1.Substring(0, 3);
                        string pick3_3 = compatible2.Substring(0, 3);
                        
                        // 5. Validar según la regla de los Pick3
                        bool isValid = true;
                        
                        // Contar posiciones con los 3 dígitos iguales
                        int equalPositions = 0;
                        for (int pos = 0; pos < 3; pos++)
                        {
                            if (pick3_1[pos] == pick3_2[pos] && pick3_2[pos] == pick3_3[pos])
                            {
                                equalPositions++;
                            }
                        }
                        
                        // Para posiciones no-iguales, reunir todos los dígitos
                        var digitsFromNonEqualPositions = new List<char>();
                        
                        for (int pos = 0; pos < 3; pos++)
                        {
                            // Solo si NO es una posición con los 3 iguales
                            if (!(pick3_1[pos] == pick3_2[pos] && pick3_2[pos] == pick3_3[pos]))
                            {
                                digitsFromNonEqualPositions.Add(pick3_1[pos]);
                                digitsFromNonEqualPositions.Add(pick3_2[pos]);
                                digitsFromNonEqualPositions.Add(pick3_3[pos]);
                            }
                        }
                        
                        // Verificar que todos los dígitos en posiciones no-iguales sean diferentes
                        if (digitsFromNonEqualPositions.Count > 0)
                        {
                            // Contar dígitos únicos
                            var uniqueDigits = new HashSet<char>();
                            foreach (char digit in digitsFromNonEqualPositions)
                            {
                                uniqueDigits.Add(digit);
                            }
                            
                            // Si hay menos únicos que totales, hay repeticiones
                            if (uniqueDigits.Count != digitsFromNonEqualPositions.Count)
                            {
                                isValid = false;
                            }
                        }
                        
                        // 6. Si pasa la validación de Pick3, verificar nueva regla del NextPick3
                        if (isValid)
                        {
                            // NUEVA VALIDACIÓN: Los últimos 3 dígitos del compatible2 (NextPick3) 
                            // no pueden tener dígitos repetidos
                            string nextPick3 = compatible2.Substring(7, 3);  // Últimos 3 dígitos
                            
                            bool nextPick3Valid = true;
                            
                            // Verificar que los 3 dígitos sean diferentes
                            if (nextPick3.Length == 3)
                            {
                                // Usar HashSet para contar dígitos únicos
                                var uniqueNextPickDigits = new HashSet<char>();
                                foreach (char digit in nextPick3)
                                {
                                    uniqueNextPickDigits.Add(digit);
                                }
                                
                                // Si hay menos de 3 dígitos únicos, hay repetición
                                if (uniqueNextPickDigits.Count < 3)
                                {
                                    nextPick3Valid = false;
                                }
                            }
                            else
                            {
                                nextPick3Valid = false;  // Si no tiene 3 dígitos, es inválido
                            }
                            
                            // Solo agregar si pasa TODAS las validaciones
                            if (nextPick3Valid)
                            {
                                // Orden del cuarteto:
                                // 1. Segundo (compatible1)
                                // 2. Tercero (compatible2)  ← Este debe tener NextPick3 sin dígitos repetidos
                                // 3. Primero (original)
                                // 4. Tercero nuevamente (compatible2)
                                allValidCuartetos.Add((
                                    compatible1,    // Primero del cuarteto
                                    compatible2,    // Segundo del cuarteto (validar NextPick3)
                                    original,       // Tercero del cuarteto
                                    compatible2     // Cuarto del cuarteto (repetición del tercero)
                                ));
                            }
                        }
                    }
                }
            }
        }
        
        return allValidCuartetos;
    }

    public List<(string First, string Second, string Third, string Fourth)> FilterCuartetosByCounters(
        List<(string First, string Second, string Third, string Fourth)> cuartetos,
        string refPick4MatchesRow1Row2,
        string refCodingMatchesRow1Row2,
        string refPick4MatchesRow3Row4,
        string refCodingMatchesRow3Row4)   
    {
        var filteredCuartetos = new List<(string, string, string, string)>();
        
        foreach (var cuarteto in cuartetos)
        {
            // Los 4 FullNumbers del cuarteto
            string row1Full = cuarteto.First;   // Primera fila
            string row2Full = cuarteto.Second;  // Segunda fila (tiene restricción de NextPick3)
            string row3Full = cuarteto.Third;   // Tercera fila
            string row4Full = cuarteto.Fourth;  // Cuarta fila = Segunda fila
            
            // 1. Extraer Pick4 de cada fila (dígitos 3-6)
            string row1Pick4 = row1Full.Substring(3, 4);
            string row2Pick4 = row2Full.Substring(3, 4);
            string row3Pick4 = row3Full.Substring(3, 4);
            string row4Pick4 = row4Full.Substring(3, 4);
            
            // 2. Calcular Coding de cada fila (Pick3 + Pick4, dígitos únicos ordenados)
            string row1Coding = CalculateCoding(row1Full.Substring(0, 3), row1Pick4);
            string row2Coding = CalculateCoding(row2Full.Substring(0, 3), row2Pick4);
            string row3Coding = CalculateCoding(row3Full.Substring(0, 3), row3Pick4);
            string row4Coding = CalculateCoding(row4Full.Substring(0, 3), row4Pick4);
            
            // 3. Calcular contadores
            
            // Pick4MatchesRow1Row2: Cuántos dígitos de Pick4 coinciden entre fila 1 y 2
            int pick4MatchesRow1Row2 = CountMatches(row1Pick4, row2Pick4);
            
            // CodingMatchesRow1Row2: Cuántos dígitos de Coding coinciden entre fila 1 y 2
            int codingMatchesRow1Row2 = CountMatches(row1Coding, row2Coding);
            
            // Pick4MatchesRow3Row4: Cuántos dígitos de Pick4 coinciden entre fila 3 y 4
            int pick4MatchesRow3Row4 = CountMatches(row3Pick4, row4Pick4);
            
            // CodingMatchesRow3Row4: Cuántos dígitos de Coding coinciden entre fila 3 y 4
            int codingMatchesRow3Row4 = CountMatches(row3Coding, row4Coding);
            
            // 4. Convertir a formato de contador (ej: "1C")
            string cuartetoPick4MatchesRow1Row2 = $"{pick4MatchesRow1Row2}C";
            string cuartetoCodingMatchesRow1Row2 = $"{codingMatchesRow1Row2}C";
            string cuartetoPick4MatchesRow3Row4 = $"{pick4MatchesRow3Row4}C";
            string cuartetoCodingMatchesRow3Row4 = $"{codingMatchesRow3Row4}C";
            
            // 5. Comparar con los contadores de referencia
            bool countersMatch = 
                cuartetoPick4MatchesRow1Row2 == refPick4MatchesRow1Row2 &&
                cuartetoCodingMatchesRow1Row2 == refCodingMatchesRow1Row2 &&
                cuartetoPick4MatchesRow3Row4 == refPick4MatchesRow3Row4 &&
                cuartetoCodingMatchesRow3Row4 == refCodingMatchesRow3Row4;
            
            // 6. Si coinciden todos los contadores, agregar a resultados
            if (countersMatch)
            {
                filteredCuartetos.Add(cuarteto);
            }
        }
        
        return filteredCuartetos;
    }

    // Función auxiliar para calcular Coding
    private string CalculateCoding(string pick3, string pick4)
    {
        // Unir Pick3 + Pick4, tomar dígitos únicos, ordenar
        var allDigits = (pick3 + pick4).Where(char.IsDigit).Distinct().OrderBy(c => c);
        return new string(allDigits.ToArray());
    }

    // Función auxiliar para contar coincidencias entre dos strings
    private int CountMatches(string str1, string str2)
    {
        int matches = 0;
        foreach (char digit in str1)
        {
            if (str2.Contains(digit))
            {
                matches++;
            }
        }
        return matches;
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

    private static bool TryBuildFirstAnalysisGuide(FilteredCodificacion codificacion, out GuideInfo guide, out List<AnalysisRow> rows)
    {
        guide = new GuideInfo();
        rows = new List<AnalysisRow>();

        var pick3 = codificacion.Pick3.Trim();
        var pick4 = codificacion.Pick4.Trim();

        if (pick3.Length != 3 || pick4.Length != 4)
        {
            return false;
        }

        if (pick3.Distinct().Count() != 3 || pick4.Distinct().Count() != 4)
        {
            return false;
        }

        var commonDigits = pick3.Intersect(pick4).ToList();
        if (commonDigits.Count != 1)
        {
            return false;
        }

        char repeatedDigit = commonDigits[0];
        int posP3 = pick3.IndexOf(repeatedDigit, StringComparison.Ordinal) + 1;
        int posP4 = pick4.IndexOf(repeatedDigit, StringComparison.Ordinal) + 1;

        var guideDate = codificacion.Date;
        var guideDrawTime = codificacion.DrawTime;

        var hits = DrawRepository.FindPositionMatches(posP3, posP4);

        rows = hits
            .Where(h => !IsSamePattern(h.Pick3, h.Pick4, pick3, pick4))
            .Where(h => IsBeforeGuide(h.Date, h.DrawTime, guideDate, guideDrawTime))
            .Select(h => new
            {
                Hit = h,
                NextPick3 = DrawRepository.GetNextPick3Number(h.Date, h.DrawTime)
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.NextPick3))
            .Select(x => new AnalysisRow
            {
                IsChecked = false,
                Date = x.Hit.Date.ToString("yyyy-MM-dd"),
                DrawTime = x.Hit.DrawTime == "M" ? "☀️" : "🌙",
                Pick3 = x.Hit.Pick3,
                Pick4 = x.Hit.Pick4,
                NextPick3 = x.NextPick3!,
                Coding = BuildCoding(x.Hit.Pick3, x.Hit.Pick4)
            })
            .ToList();

        guide = new GuideInfo
        {
            Pick3 = pick3,
            Pick4 = pick4,
            NextPick3 = DrawRepository.GetNextPick3Number(guideDate, guideDrawTime) ?? "",
            Coding = BuildCoding(pick3, pick4),
            DateText = guideDate.ToString("yyyy-MM-dd"),
            DrawIcon = DrawTimeToIcon(guideDrawTime),
            RepPosP3 = posP3,
            RepPosP4 = posP4
        };

        return rows.Count > 0;
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


    private static string DrawTimeToIcon(string drawTime)
    {
        return drawTime switch
        {
            "M" => "☀️",
            "E" => "🌙",
            _ => ""
        };
    }

    private static string BuildCoding(string pick3, string pick4)
    {
        return new string((pick3 + pick4)
            .Where(char.IsDigit)
            .Distinct()
            .OrderBy(c => c)
            .ToArray());
    }

    private static bool IsSamePattern(string pick3, string pick4, string inputPick3, string inputPick4)
    {
        return string.Equals(pick3, inputPick3, StringComparison.Ordinal)
            && string.Equals(pick4, inputPick4, StringComparison.Ordinal);
    }

    private static bool IsBeforeGuide(DateTime hitDate, string hitDrawTime, DateTime guideDate, string? guideDrawTime)
    {
        if (hitDate.Date < guideDate.Date)
        {
            return true;
        }

        if (hitDate.Date > guideDate.Date)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(guideDrawTime))
        {
            return false;
        }

        return guideDrawTime == "E" && hitDrawTime == "M";
    }

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
