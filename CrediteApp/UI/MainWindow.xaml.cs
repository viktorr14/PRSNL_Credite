using LoanSubsystem;
using LoanSubsystem.Models;
using WebCrawlSubsystem;
using WebCrawlSubsystem.Models;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Data;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace UI
{
    public partial class MainWindow : Window, IWebCrawlStatusObserver
    {
        private readonly LoanManager loanManager;

        private WebCrawlResult webCrawlResult;

        private ObservableCollection<Loan> loanCollection = new ObservableCollection<Loan>();
        private ObservableCollection<Instalment> instalmentCollection = new ObservableCollection<Instalment>();
        private ObservableCollection<AdditionalRepayment> additionalRepaymentCollection = new ObservableCollection<AdditionalRepayment>();

        public MainWindow(LoanManager loanManager)
        {
            this.loanManager = loanManager;

            DateTime nextTick = DateTime.Now;
            if (nextTick.Hour >= 14)
            {
                nextTick = nextTick.AddDays(1);
            }
            nextTick = new DateTime(nextTick.Year, nextTick.Month, nextTick.Day, 14, 00, 00);
            Timer timer = new Timer(new TimerCallback(OnDayChange), null, nextTick - DateTime.Now, TimeSpan.FromHours(24));

            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            loadingScreenGrid.Visibility = Visibility.Visible;

            webCrawlResult = await Task.Run(() => WebCrawlManager.DetailWebCrawlResult());

            PopulateLoanCollection(loanManager.ListCredits);
            DisplayWebCrawlResult();
            infoLabel.Content = $"Informații (Azi, {FormatDate(DateTime.Now)})";

            loadingScreenGrid.Visibility = Visibility.Hidden;
        }

        private async void OnDayChange(object state)
        {
            loadingScreenGrid.Visibility = Visibility.Visible;

            webCrawlResult = await Task.Run(() => WebCrawlManager.DetailWebCrawlResult());

            PopulateLoanCollection(loanManager.ListCredits);
            DisplayWebCrawlResult();
            infoLabel.Content = $"Informații (Azi, {FormatDate(DateTime.Now)})";

            loadingScreenGrid.Visibility = Visibility.Hidden;
        }

        private void DisplayWebCrawlResult()
        {
            euroExchangeRateDateLabel.Content = FormatDate(webCrawlResult.EuroExchangeRate.Date);
            euroExchangeRateValueLabel.Content = FormatExchangeRate(webCrawlResult.EuroExchangeRate.Value);
            euroExchangeRateDifferenceLabel.Content = FormatExchangeRateChange(webCrawlResult.EuroExchangeRate.PreviousDateChange);

            if (webCrawlResult.EuroExchangeRate.PreviousDateChange > 0)
            {
                euroExchangeRateDifferenceLabel.Foreground = Brushes.Crimson;
                euroExchangeRateValueLabel.Background = Brushes.Salmon;
            }
            else
            {
                euroExchangeRateDifferenceLabel.Foreground = Brushes.LimeGreen;
            }

            if (webCrawlResult.EuroExchangeRate.Date != DateTime.Now.Date)
            {
                euroExchangeRateOutdatedWarningLabel.Visibility = Visibility.Visible;
            }

            irccTableListView.ItemsSource = webCrawlResult.QuarterlyIndices;
        }

        private void PopulateLoanCollection(Loan[] loans, int selectIndex = 0)
        {
            Loan[] loansList = loans.Prepend(new Loan { Id = -1, Name = "Credit nou..." }).ToArray();

            loanCollection = new ObservableCollection<Loan>(loans);
            loanSelectionComboBox.DataContext = loanCollection;
            loanSelectionComboBox.SelectedIndex = selectIndex > loansList.Length - 1 ? loansList.Length - 1 : selectIndex;
        }

        private void PopulateInstalmentCollection(Instalment[] instalmentEntries)
        {
            instalmentCollection = new ObservableCollection<Instalment>(instalmentEntries);
            instalmentTableDataGrid.DataContext = instalmentCollection;

            int currentDateItemIndex = instalmentEntries.Select((entry, index) => (entry.Date, index)).FirstOrDefault(tuple => tuple.Date.AddMonths(-1) < DateTime.Today && DateTime.Today <= tuple.Date).index;

            ScrollViewer scrollViewer = GetScrollViewer(instalmentTableDataGrid) as ScrollViewer;
            scrollViewer.ScrollToVerticalOffset(currentDateItemIndex);
        }

        private static DependencyObject GetScrollViewer(DependencyObject dependencyObject)
        {
            if (dependencyObject is ScrollViewer)
                return dependencyObject;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(dependencyObject); i++)
            {
                var child = VisualTreeHelper.GetChild(dependencyObject, i);

                var result = GetScrollViewer(child);
                if (result == null)
                {
                    continue;
                }
                else
                {
                    return result;
                }
            }
            return null;
        }

        private void LoanSelectionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(loanSelectionComboBox.SelectedItem is Loan selectedLoan))
            {
                return;
            }

            ToggleCreditReimbursementDetailsVisibility(selectedLoan.Id > 0);
            SetAdditionalLoanRepaymentControlsEnabled(false);
            DisplayCreditDetails(selectedLoan);

            if (selectedLoan.Id > 0)
            {
                RepaymentDetails repaymentDetails = LoanManager.GetRepaymentDetails(selectedLoan, webCrawlResult.QuarterlyIndices);

                PopulateInstalmentCollection(repaymentDetails.Instalments);

                loanTotalCostLabel.Content = $"Cost total credit: {string.Format(repaymentDetails.TotalCost.ToString("F2"))}";
                savingsLabel.Content = $"Economii din plățile anticipate: {string.Format(repaymentDetails.AdditonalRepaymentsSavings.ToString("F2"))}";
            }
        }

        private void ToggleCreditReimbursementDetailsVisibility(bool visible)
        {
            repaymentTableGroupBox.Visibility = loanCostGroupBox.Visibility = additionalLoanRepaymentGroupBox.Visibility = visible ? Visibility.Visible : Visibility.Hidden;
        }

        private void DisplayCreditDetails(Loan creditDetails)
        {
            loanNameTextBox.Text = creditDetails.Name;
            loanValueTextBox.Text = creditDetails.Sum != 0 ? creditDetails.Sum.ToString() : string.Empty;
            loanStartDateDatePicker.SelectedDate = creditDetails.StartDate != DateTime.MinValue ? creditDetails.StartDate : null;
            loanDurationTextBox.Text = creditDetails.Duration != 0 ? creditDetails.Duration.ToString() : string.Empty;
            loanFixedInterestRateTextBox.Text = creditDetails.InterestRate != 0 ? creditDetails.InterestRate.ToString() : string.Empty;
            loanFixedInterestTypeRadioButton.IsChecked = creditDetails.Id > 0 && creditDetails.InterestType == InterestType.Fixed;
            loanVariableInterestTypeRadioButton.IsChecked = creditDetails.Id > 0 && creditDetails.InterestType == InterestType.Variable;
            loanMonthlyDueDateTextBox.Text = creditDetails.MonthlyDueDay != 0 ? creditDetails.MonthlyDueDay.ToString() : string.Empty;
            loanAdditionalLoanRepaymentCommissionTextBox.Text = creditDetails.AdditionalRepaymentCommission.ToString();
        }

        private void SaveLoanButton_Click(object sender, RoutedEventArgs e)
        {
            List<string> errors = new List<string>();

            Loan creditDetails = new Loan
            {
                Id = GetLoanId(),
                Name = ValidateLoanName(loanNameTextBox.Text, errors),
                Sum = ValidateDecimalValueInput("Valoarea creditului", loanValueTextBox.Text, errors),
                StartDate = ValidateDateIsSelected(loanStartDateDatePicker.SelectedDate, "de inceput", errors).Date,
                Duration = ValidateIntegerValueInput("Perioada", loanDurationTextBox.Text, new Tuple<int, int>(1, 360), errors),
                InterestType = ValidateInterestTypeIsSelected(errors),
                InterestRate = ValidateDecimalValueInput("Rata dobanzii fixe", loanFixedInterestRateTextBox.Text, errors),
                MonthlyDueDay = ValidateIntegerValueInput("Scadența lunară", loanMonthlyDueDateTextBox.Text, new Tuple<int, int>(1, 28), errors),
                AdditionalRepaymentCommission = ValidateDecimalValueInput("Comisionul de rambursare anticipată", loanAdditionalLoanRepaymentCommissionTextBox.Text, errors),
                AdditionalRepayments = new List<AdditionalRepayment>()
            };

            if (errors.Any())
            {
                DisplayErrors("Erori la salvarea creditului.", errors);
                return;
            }

            loanManager.SaveLoan(creditDetails);

            PopulateLoanCollection(loanManager.ListCredits, loanCollection.Count - 1);
        }

        private int GetLoanId()
        {
            if ((loanSelectionComboBox.SelectedItem as Loan).Id > 0)
            {
                return (loanSelectionComboBox.SelectedItem as Loan).Id;
            }

            int lastID = loanCollection.OrderBy(cd => cd.Id).Last().Id;

            return lastID < 0 ? 1 : lastID + 1;
        }

        private void InstalmentTableDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (instalmentTableDataGrid.SelectedIndex == -1)
            {
                SetAdditionalLoanRepaymentControlsEnabled(false);
                return;
            }

            SetAdditionalLoanRepaymentControlsEnabled(true);

            Instalment selectedInstalment = instalmentTableDataGrid.SelectedItem as Instalment;
            AdditionalRepayment[] selectedInstalmentAdditionalRepayments = selectedInstalment.AdditionalLoanRepayments;

            additionalLoanRepaymentDateDatePicker.DisplayDateStart = selectedInstalment.Date.AddMonths(-1).AddDays(1);
            additionalLoanRepaymentDateDatePicker.DisplayDateEnd = selectedInstalment.Date;
            additionalLoanRepaymentDateDatePicker.DisplayDate = selectedInstalment.Date;

            PopulateAdditionalLoanRepaymentsCollection(selectedInstalmentAdditionalRepayments);
        }

        private void SetAdditionalLoanRepaymentControlsEnabled(bool enabled)
        {
            additionalLoanRepaymentSelectionComboBox.IsEnabled = enabled;
            additionalLoanRepaymentDateDatePicker.IsEnabled = enabled;
            additionalLoanRepaymentSumTextBox.IsEnabled = enabled;
            saveAdditionalLoanRepaymentButton.IsEnabled = enabled;
            additionalLoanRepaymentPeriodReductionTypeRadioButton.IsEnabled = enabled;
            additionalLoanRepaymentPaymentReductionTypeRadioButton.IsEnabled = enabled;
        }

        private void PopulateAdditionalLoanRepaymentsCollection(AdditionalRepayment[] additionalRepayments, int selectIndex = 0)
        {
            AdditionalRepayment[] repayments = additionalRepayments.Select(repayment => new AdditionalRepayment
            {
                Name = FormatDate(repayment.Date),
                Date = repayment.Date,
                Sum = repayment.Sum,
                Type = repayment.Type
            }).Prepend(new AdditionalRepayment { Name = "Plată nouă..." }).ToArray();

            this.additionalRepaymentCollection = new ObservableCollection<AdditionalRepayment>(repayments);
            additionalLoanRepaymentSelectionComboBox.DataContext = this.additionalRepaymentCollection;
            additionalLoanRepaymentSelectionComboBox.SelectedIndex = selectIndex > repayments.Length - 1 ? repayments.Length - 1 : selectIndex;
        }

        private void AdditionalLoanRepaymentSelectionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(additionalLoanRepaymentSelectionComboBox.SelectedItem is AdditionalRepayment selectedAdditionalRepayment))
            {
                return;
            }

            if (additionalLoanRepaymentSelectionComboBox.SelectedIndex == 0)
            {
                deleteAdditionalLoanRepaymentButton.Visibility = Visibility.Hidden;
                saveAdditionalLoanRepaymentButton.Content = "Adaugă";

                additionalLoanRepaymentSumTextBox.Clear();
                additionalLoanRepaymentDateDatePicker.IsEnabled = true;
                additionalLoanRepaymentDateDatePicker.SelectedDate = null;
                additionalLoanRepaymentPeriodReductionTypeRadioButton.IsChecked = false;
                additionalLoanRepaymentPaymentReductionTypeRadioButton.IsChecked = false;

                return;
            }

            deleteAdditionalLoanRepaymentButton.Visibility = Visibility.Visible;
            saveAdditionalLoanRepaymentButton.Content = "Salvează";

            additionalLoanRepaymentDateDatePicker.IsEnabled = false;
            additionalLoanRepaymentDateDatePicker.SelectedDate = selectedAdditionalRepayment.Date;
            additionalLoanRepaymentSumTextBox.Text = selectedAdditionalRepayment.Sum.ToString();
            additionalLoanRepaymentPeriodReductionTypeRadioButton.IsChecked = selectedAdditionalRepayment.Type == AdditionalRepaymentType.PeriodReduction;
            additionalLoanRepaymentPaymentReductionTypeRadioButton.IsChecked = selectedAdditionalRepayment.Type == AdditionalRepaymentType.PaymentReduction;
        }

        private void DeleteAdditionalLoanRepaymentButton_Click(object sender, RoutedEventArgs e)
        {
            deleteAdditionalLoanRepaymentButton.Visibility = Visibility.Hidden;

            DateTime additionalRepaymentDate = (additionalLoanRepaymentSelectionComboBox.SelectedItem as AdditionalRepayment).Date.Date;

            loanManager.DeleteAdditionalRepayment(loanSelectionComboBox.SelectedItem as Loan, additionalRepaymentDate);

            additionalLoanRepaymentDateDatePicker.SelectedDate = null;
            additionalLoanRepaymentPaymentReductionTypeRadioButton.IsChecked = false;
            additionalLoanRepaymentPeriodReductionTypeRadioButton.IsChecked = false;
            additionalLoanRepaymentSumTextBox.Clear();

            PopulateLoanCollection(loanManager.ListCredits, loanSelectionComboBox.SelectedIndex);
        }

        private void SaveAdditionalLoanRepaymentButton_Click(object sender, RoutedEventArgs e)
        {
            List<string> errors = new List<string>();

            SetAdditionalLoanRepaymentControlsEnabled(false);

            AdditionalRepayment additionalRepayment = new AdditionalRepayment
            {
                Name = FormatDate(ValidateDateIsSelected(additionalLoanRepaymentDateDatePicker.SelectedDate, "plății anticipate", errors)),
                Date = ValidateDateIsSelected(additionalLoanRepaymentDateDatePicker.SelectedDate, "plății anticipate", errors).Date,
                Sum = ValidateDecimalValueInput("Suma", additionalLoanRepaymentSumTextBox.Text, errors),
                Type = ValidateAdditionalLoanRepaymentTypeIsSelected(errors),
            };

            if (errors.Any())
            {
                DisplayErrors("Erori la salvarea plății anticipate.", errors);

                SetAdditionalLoanRepaymentControlsEnabled(true);
            }
            else
            {
                loanManager.SaveAdditionalRepayment(loanSelectionComboBox.SelectedItem as Loan, additionalRepayment);

                additionalLoanRepaymentDateDatePicker.SelectedDate = null;
                additionalLoanRepaymentPaymentReductionTypeRadioButton.IsChecked = false;
                additionalLoanRepaymentPeriodReductionTypeRadioButton.IsChecked = false;
                additionalLoanRepaymentSumTextBox.Clear();

                PopulateLoanCollection(loanManager.ListCredits, loanSelectionComboBox.SelectedIndex);
            }
        }

        #region Input Validation

        private static string ValidateLoanName(string creditName, List<string> errors)
        {
            if (creditName.Contains("credit nou", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add("Numele creditului seamănă cu cel implicit pentru un credit nou. Alege un nume mai sugestiv.");
                return string.Empty;
            }

            return creditName;
        }

        private static decimal ValidateDecimalValueInput(string inputName, string inputValue, List<string> errors)
        {
            if (decimal.TryParse(inputValue, out decimal parsedValue))
                return parsedValue;

            errors.Add($"\"{inputName}\" trebuie să fie un număr real.");
            return 0M;
        }

        private static int ValidateIntegerValueInput(string inputName, string inputValue, Tuple<int, int> limitInterval, List<string> errors)
        {
            if (int.TryParse(inputValue, out int parsedValue))
            {
                if (limitInterval != null && (parsedValue < limitInterval.Item1 || limitInterval.Item2 < parsedValue))
                {
                    errors.Add($"\"{inputName}\" trebuie să se încadreze între limitele {limitInterval.Item1} și {limitInterval.Item2}.");
                }

                return parsedValue;
            }

            errors.Add($"\"{inputName}\" trebuie să fie un număr întreg.");
            return 0;
        }

        private static DateTime ValidateDateIsSelected(DateTime? selectedDate, string dateName, List<string> errors)
        {
            if (selectedDate.HasValue)
            {
                return selectedDate.Value;
            }

            errors.Add($"Nu ai selectat data {dateName}.");
            return DateTime.MinValue;
        }

        private InterestType ValidateInterestTypeIsSelected(List<string> errors)
        {
            if (loanFixedInterestTypeRadioButton.IsChecked.HasValue && loanFixedInterestTypeRadioButton.IsChecked.Value)
            {
                return InterestType.Fixed;
            }

            if (loanVariableInterestTypeRadioButton.IsChecked.HasValue && loanVariableInterestTypeRadioButton.IsChecked.Value)
            {
                return InterestType.Variable;
            }

            errors.Add("Nu ai selectat tipul de dobândă.");
            return default;
        }

        private AdditionalRepaymentType ValidateAdditionalLoanRepaymentTypeIsSelected(List<string> errors)
        {
            if (additionalLoanRepaymentPeriodReductionTypeRadioButton.IsChecked.HasValue && additionalLoanRepaymentPeriodReductionTypeRadioButton.IsChecked.Value)
            {
                return AdditionalRepaymentType.PeriodReduction;
            }

            if (additionalLoanRepaymentPaymentReductionTypeRadioButton.IsChecked.HasValue && additionalLoanRepaymentPaymentReductionTypeRadioButton.IsChecked.Value)
            {
                return AdditionalRepaymentType.PaymentReduction;
            }

            errors.Add("Nu ai selectat opțiunea plății anticipate.");
            return default;
        }

        private static void DisplayErrors(string title, List<string> errors)
        {
            string errorMessage = errors[0];
            for (int index = 1; index < errors.Count; index++)
            {
                errorMessage += $"\r\n\r\n{errors[index]}";
            }

            MessageBox.Show(errorMessage, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        #endregion

        #region Formatting

        private static string FormatDate(DateTime date)
        {
            return date.ToString("dd.MM.yyyy");
        }

        private static string FormatExchangeRate(decimal value)
        {
            return value.ToString("0.#### Lei");
        }

        private static string FormatExchangeRateChange(decimal value)
        {
            return value.ToString("+0.####;-0.####;0");
        }

        #endregion
    }

    #region Converters

    public class RepaymentEntryDateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            DateTime? date = value as DateTime?;
            if (!date.HasValue)
            {
                return Brushes.Transparent;
            }

            return date.Value.AddMonths(-1) < DateTime.Today && DateTime.Today <= date.Value ? Brushes.YellowGreen : Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    #endregion
}