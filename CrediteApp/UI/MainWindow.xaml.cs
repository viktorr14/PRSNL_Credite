using Credit;
using Credit.Models;
using BNR;
using BNR.Models;
using UI.Misc;
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
    public partial class MainWindow : Window
    {
        private CreditManager creditManager;

        private BNRResult bnrResult;

        private ObservableCollection<CreditDetails> creditsDetails = new ObservableCollection<CreditDetails>();
        private ObservableCollection<RepaymentEntry> repaymentEntries = new ObservableCollection<RepaymentEntry>();
        private ObservableCollection<AdditionalLoanRepayment> additionalLoanRepayments = new ObservableCollection<AdditionalLoanRepayment>();

        public MainWindow(CreditManager creditManager)
        {
            this.creditManager = creditManager;

            DateTime nextTick = DateTime.Now;
            if (nextTick.Hour >= 14)
            {
                nextTick = nextTick.AddDays(1);
            }
            nextTick = new DateTime(nextTick.Year, nextTick.Month, nextTick.Day, 14, 00, 00);
            Timer timer = new Timer(new TimerCallback(OnDayChange), null, nextTick - DateTime.Now, TimeSpan.FromHours(24));

            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Task<BNRResult> task = Task.Run(() => BNRManager.DetailBNRResult());

            loadingScreenGrid.Visibility = Visibility.Visible;

            Task.WaitAll(task);

            bnrResult = task.Result;

            PopulateCreditCollection(creditManager.ListCredits);

            infoLabel.Content = $"Informații (Azi, {FormatDate(DateTime.Now)})";

            DisplayBNRInformation();

            loadingScreenGrid.Visibility = Visibility.Hidden;
        }

        private void OnDayChange(object state)
        {
            Task<BNRResult> task = Task.Run(() => BNRManager.DetailBNRResult());

            loadingScreenGrid.Visibility = Visibility.Visible;

            Task.WaitAll(task);

            bnrResult = task.Result;

            PopulateCreditCollection(creditManager.ListCredits);

            infoLabel.Content = $"Informații (Azi, {FormatDate(DateTime.Now)})";

            DisplayBNRInformation();

            loadingScreenGrid.Visibility = Visibility.Hidden;
        }

        private void DisplayBNRInformation()
        {
            euroDateLabel.Content = FormatDate(bnrResult.EuroExchangeRate.Date);
            euroExchangeRateLabel.Content = FormatExchangeRate(bnrResult.EuroExchangeRate.Value);
            euroPreviousDateChangeLabel.Content = FormatExchangeRateChange(bnrResult.EuroExchangeRate.PreviousDateChange);

            if (bnrResult.EuroExchangeRate.PreviousDateChange > 0)
            {
                euroPreviousDateChangeLabel.Foreground = Brushes.Crimson;
                euroExchangeRateLabel.Background = Brushes.Salmon;
            }
            else
            {
                euroPreviousDateChangeLabel.Foreground = Brushes.LimeGreen;
            }

            if (bnrResult.EuroExchangeRate.Date != DateTime.Now.Date)
            {
                euroExchangeRateNotUpToDateWarningLabel.Visibility = Visibility.Visible;
            }

            irccTableListView.ItemsSource = bnrResult.QuarterlyIndices;
        }

        private void PopulateCreditCollection(CreditDetails[] creditsDetails, int selectIndex = 0)
        {
            CreditDetails[] credits = creditsDetails.Prepend(new CreditDetails { Id = -1, Name = "Credit nou..." }).ToArray();

            this.creditsDetails = new ObservableCollection<CreditDetails>(credits);
            creditSelectionComboBox.DataContext = this.creditsDetails;
            creditSelectionComboBox.SelectedIndex = selectIndex > credits.Length - 1 ? credits.Length - 1 : selectIndex;
        }

        private void PopulateReimbursementCollection(RepaymentEntry[] repaymentEntries)
        {
            this.repaymentEntries = new ObservableCollection<RepaymentEntry>(repaymentEntries);
            repaymentTableDataGrid.DataContext = this.repaymentEntries;

            int currentDateItemIndex = repaymentEntries.Select((entry, index) => (entry.Date, index)).First(tuple => tuple.Date.Year == DateTime.Now.Year && tuple.Date.Month == DateTime.Now.Month).index;

            ScrollViewer scrollViewer = GetScrollViewer(repaymentTableDataGrid) as ScrollViewer;
            scrollViewer.ScrollToTop();
            for (int scroll = 0; scroll < currentDateItemIndex; scroll++)
            {
                scrollViewer.LineDown();
            }
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

        private void CreditSelectionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(creditSelectionComboBox.SelectedItem is CreditDetails selectedCredit))
            {
                return;
            }

            ToggleCreditReimbursementDetailsVisibility(selectedCredit.Id > 0);
            SetAdditionalLoanRepaymentControlsEnabled(false);
            DisplayCreditDetails(selectedCredit);

            if (selectedCredit.Id > 0)
            {
                RepaymentDetails repaymentDetails = CreditManager.GetRepaymentDetails(selectedCredit, bnrResult.QuarterlyIndices);

                PopulateReimbursementCollection(repaymentDetails.RepaymentEntries);

                totalCostLabel.Content = $"Cost total credit: {string.Format(repaymentDetails.TotalCost.ToString("F2"))}";
                savingsLabel.Content = $"Economii din plățile anticipate: {string.Format(repaymentDetails.AdditonalRepaymentsSavings.ToString("F2"))}";
            }
        }

        private void ToggleCreditReimbursementDetailsVisibility(bool visible)
        {
            repaymentTableGroupBox.Visibility = creditCostGroupBox.Visibility = additionalLoanRepaymentGroupBox.Visibility = visible ? Visibility.Visible : Visibility.Hidden;
        }

        private void DisplayCreditDetails(CreditDetails creditDetails)
        {
            creditNameTextBox.Text = creditDetails.Name;
            creditValueTextBox.Text = creditDetails.Sum != 0 ? creditDetails.Sum.ToString() : string.Empty;
            creditStartDateDatePicker.SelectedDate = creditDetails.StartDate != DateTime.MinValue ? (DateTime?)creditDetails.StartDate : null;
            creditDurationTextBox.Text = creditDetails.Duration != 0 ? creditDetails.Duration.ToString() : string.Empty;
            creditFixedInterestRateTextBox.Text = creditDetails.InterestRate != 0 ? creditDetails.InterestRate.ToString() : string.Empty;
            creditFixedInterestTypeRadioButton.IsChecked = creditDetails.Id > 0 && creditDetails.InterestType == InterestType.Fixed;
            creditVariableInterestTypeRadioButton.IsChecked = creditDetails.Id > 0 && creditDetails.InterestType == InterestType.Variable;
            creditMonthlyDueDateTextBox.Text = creditDetails.MonthlyDueDay != 0 ? creditDetails.MonthlyDueDay.ToString() : string.Empty;
            creditAdditionalLoanRepaymentCommissionTextBox.Text = creditDetails.AdditionalLoanRepaymentCommission != 0 ? creditDetails.AdditionalLoanRepaymentCommission.ToString() : string.Empty;
        }

        private void SaveCreditButton_Click(object sender, RoutedEventArgs e)
        {
            List<string> errors = new List<string>();

            CreditDetails creditDetails = new CreditDetails
            {
                Id = GetCreditId(),
                Name = ValidateCreditName(creditNameTextBox.Text, errors),
                Sum = ValidateDecimalValueInput("Valoarea creditului", creditValueTextBox.Text, errors),
                StartDate = ValidateDateIsSelected(creditStartDateDatePicker.SelectedDate, "de inceput", errors).ToDateOnly(),
                Duration = ValidateIntegerValueInput("Perioada", creditDurationTextBox.Text, new Tuple<int, int>(1, 360), errors),
                InterestType = ValidateInterestTypeIsSelected(errors),
                InterestRate = ValidateDecimalValueInput("Rata dobanzii fixe", creditFixedInterestRateTextBox.Text, errors),
                MonthlyDueDay = ValidateIntegerValueInput("Scadența lunară", creditMonthlyDueDateTextBox.Text, new Tuple<int, int>(1, 28), errors),
                AdditionalLoanRepaymentCommission = ValidateDecimalValueInput("Comisionul de rambursare anticipată", creditAdditionalLoanRepaymentCommissionTextBox.Text, errors),
                AdditionalLoanRepayments = new List<AdditionalLoanRepayment>()
            };

            if (errors.Any())
            {
                DisplayErrors("Erori la salvarea creditului.", errors);
                return;
            }

            creditManager.SaveCredit(creditDetails);

            PopulateCreditCollection(creditManager.ListCredits, creditsDetails.Count - 1);
        }

        private int GetCreditId()
        {
            if ((creditSelectionComboBox.SelectedItem as CreditDetails).Id > 0)
            {
                return (creditSelectionComboBox.SelectedItem as CreditDetails).Id;
            }

            int lastID = creditsDetails.OrderBy(cd => cd.Id).Last().Id;

            return lastID < 0 ? 1 : lastID + 1;
        }

        private void ReimbursementTableDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (repaymentTableDataGrid.SelectedIndex == -1)
            {
                SetAdditionalLoanRepaymentControlsEnabled(false);
                return;
            }

            SetAdditionalLoanRepaymentControlsEnabled(true);

            RepaymentEntry selectedReimbursementEntry = repaymentTableDataGrid.SelectedItem as RepaymentEntry;
            AdditionalLoanRepayment[] selectedReimbursementEntryAdditionalLoanRepayments = selectedReimbursementEntry.AdditionalLoanRepayments;

            additionalLoanRepaymentDateDatePicker.DisplayDateStart = selectedReimbursementEntry.Date.AddMonths(-1).AddDays(1);
            additionalLoanRepaymentDateDatePicker.DisplayDateEnd = selectedReimbursementEntry.Date;
            additionalLoanRepaymentDateDatePicker.DisplayDate = selectedReimbursementEntry.Date;

            PopulateAdditionalLoanRepaymentsCollection(selectedReimbursementEntryAdditionalLoanRepayments);
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

        private void PopulateAdditionalLoanRepaymentsCollection(AdditionalLoanRepayment[] additionalLoanRepayments, int selectIndex = 0)
        {
            AdditionalLoanRepayment[] repayments = additionalLoanRepayments.Select(repayment => new AdditionalLoanRepayment
            {
                Name = FormatDate(repayment.Date),
                Date = repayment.Date,
                Sum = repayment.Sum,
                Type = repayment.Type
            }).Prepend(new AdditionalLoanRepayment { Name = "Plată nouă..." }).ToArray();

            this.additionalLoanRepayments = new ObservableCollection<AdditionalLoanRepayment>(repayments);
            additionalLoanRepaymentSelectionComboBox.DataContext = this.additionalLoanRepayments;
            additionalLoanRepaymentSelectionComboBox.SelectedIndex = selectIndex > repayments.Length - 1 ? repayments.Length - 1 : selectIndex;
        }

        private void AdditionalLoanRepaymentSelectionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(additionalLoanRepaymentSelectionComboBox.SelectedItem is AdditionalLoanRepayment selectedAdditionalLoanRepayment))
            {
                return;
            }

            if (additionalLoanRepaymentSelectionComboBox.SelectedIndex == 0)
            {
                saveAdditionalLoanRepaymentButton.Content = "Adaugă";

                additionalLoanRepaymentSumTextBox.Clear();
                additionalLoanRepaymentDateDatePicker.IsEnabled = true;
                additionalLoanRepaymentDateDatePicker.SelectedDate = null;
                additionalLoanRepaymentPeriodReductionTypeRadioButton.IsChecked = false;
                additionalLoanRepaymentPaymentReductionTypeRadioButton.IsChecked = false;

                return;
            }

            saveAdditionalLoanRepaymentButton.Content = "Salvează";

            additionalLoanRepaymentDateDatePicker.IsEnabled = false;
            additionalLoanRepaymentDateDatePicker.SelectedDate = selectedAdditionalLoanRepayment.Date;
            additionalLoanRepaymentSumTextBox.Text = selectedAdditionalLoanRepayment.Sum.ToString();
            additionalLoanRepaymentPeriodReductionTypeRadioButton.IsChecked = selectedAdditionalLoanRepayment.Type == AdditionalLoanRepaymentType.PeriodReduction;
            additionalLoanRepaymentPaymentReductionTypeRadioButton.IsChecked = selectedAdditionalLoanRepayment.Type == AdditionalLoanRepaymentType.PaymentReduction;
        }

        private void SaveAdditionalLoanRepaymentButton_Click(object sender, RoutedEventArgs e)
        {
            List<string> errors = new List<string>();

            SetAdditionalLoanRepaymentControlsEnabled(false);

            AdditionalLoanRepayment additionalLoanRepayment = new AdditionalLoanRepayment
            {
                Name = FormatDate(ValidateDateIsSelected(additionalLoanRepaymentDateDatePicker.SelectedDate, "plății anticipate", errors)),
                Date = ValidateDateIsSelected(additionalLoanRepaymentDateDatePicker.SelectedDate, "plății anticipate", errors).ToDateOnly(),
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
                creditManager.SaveAdditionalLoanRepayment(creditSelectionComboBox.SelectedItem as CreditDetails, additionalLoanRepayment);

                additionalLoanRepaymentDateDatePicker.SelectedDate = null;
                additionalLoanRepaymentSumTextBox.Clear();

                PopulateCreditCollection(creditManager.ListCredits, creditSelectionComboBox.SelectedIndex);
            }
        }

        #region Input Validation

        private string ValidateCreditName(string creditName, List<string> errors)
        {
            if (creditName.Contains("credit nou", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add("Numele creditului seamănă cu cel implicit pentru un credit nou. Alege un nume mai sugestiv.");
                return string.Empty;
            }

            return creditName;
        }

        private decimal ValidateDecimalValueInput(string inputName, string inputValue, List<string> errors)
        {
            if (decimal.TryParse(inputValue, out decimal parsedValue))
                return parsedValue;

            errors.Add($"\"{inputName}\" trebuie să fie un număr real.");
            return 0M;
        }

        private int ValidateIntegerValueInput(string inputName, string inputValue, Tuple<int, int> limitInterval, List<string> errors)
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

        private DateTime ValidateDateIsSelected(DateTime? selectedDate, string dateName, List<string> errors)
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
            if (creditFixedInterestTypeRadioButton.IsChecked.HasValue && creditFixedInterestTypeRadioButton.IsChecked.Value)
            {
                return InterestType.Fixed;
            }

            if (creditVariableInterestTypeRadioButton.IsChecked.HasValue && creditVariableInterestTypeRadioButton.IsChecked.Value)
            {
                return InterestType.Variable;
            }

            errors.Add("Nu ai selectat tipul de dobândă.");
            return default;
        }

        private AdditionalLoanRepaymentType ValidateAdditionalLoanRepaymentTypeIsSelected(List<string> errors)
        {
            if (additionalLoanRepaymentPeriodReductionTypeRadioButton.IsChecked.HasValue && additionalLoanRepaymentPeriodReductionTypeRadioButton.IsChecked.Value)
            {
                return AdditionalLoanRepaymentType.PeriodReduction;
            }

            if (additionalLoanRepaymentPaymentReductionTypeRadioButton.IsChecked.HasValue && additionalLoanRepaymentPaymentReductionTypeRadioButton.IsChecked.Value)
            {
                return AdditionalLoanRepaymentType.PaymentReduction;
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

        private string FormatDate(DateTime date)
        {
            return date.ToString("dd.MM.yyyy");
        }

        private string FormatExchangeRate(decimal value)
        {
            return value.ToString("0.#### Lei");
        }

        private string FormatExchangeRateChange(decimal value)
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

            DateTime dateNow = DateTime.Now;

            return date.Value.Year == dateNow.Year && date.Value.Month == dateNow.Month ? Brushes.YellowGreen : Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    #endregion
}