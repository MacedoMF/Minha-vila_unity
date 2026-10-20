using System;

namespace MinhaVila.Core
{
    public static class FinanceSystem
    {
        public const int MinimumLoan = 100;
        public const int MaximumLoan = 100000;
        public const double LoanTermHours = 72;
        public const double InterestIntervalHours = 24;

        public static int GetUnitPrice(ResourceType type)
        {
            switch (type)
            {
                case ResourceType.Timber: return 10;
                case ResourceType.Stone: return 6;
                case ResourceType.Iron: return 20;
                case ResourceType.Food: return 8;
                default: return 0;
            }
        }

        public static bool TryBuy(VillageState state, ResourceType type, int quantity, out string message)
        {
            message = string.Empty;
            if (quantity < 1 || quantity > 10000)
            {
                message = "Escolha uma quantidade entre 1 e 10.000.";
                return false;
            }

            int price = GetUnitPrice(type);
            long total = (long)price * quantity;
            if (price <= 0 || total > state.Gold)
            {
                message = "O tesouro não tem ouro suficiente para essa compra.";
                return false;
            }

            if (GetStock(state.Stock, type) > ResourceStock.Maximum - quantity)
            {
                message = "O depósito atingiu o limite desse recurso.";
                return false;
            }

            state.Gold -= total;
            state.SpentGold += total;
            SetStock(state.Stock, type, GetStock(state.Stock, type) + quantity);
            message = quantity + " unidades adicionadas ao depósito.";
            return true;
        }

        public static bool TryBorrow(VillageState state, int amount, out string message)
        {
            message = string.Empty;
            if (amount < MinimumLoan || amount > MaximumLoan)
            {
                message = "O empréstimo deve ser de 100 a 100.000 ouros.";
                return false;
            }

            if (state.Loan.Debt > 0)
            {
                message = "Quite o contrato atual antes de pedir outro.";
                return false;
            }

            int fee = (int)Math.Ceiling(amount / 10.0);
            state.Loan = new LoanRecord
            {
                Principal = amount,
                Fee = fee,
                Debt = amount + fee
            };
            state.Gold += amount;
            message = "Empréstimo contratado. Taxa única: " + fee + " ouros.";
            return true;
        }

        public static bool TryRepay(VillageState state, int amount, out string message)
        {
            message = string.Empty;
            if (state.Loan.Debt <= 0 || amount <= 0 || amount > state.Loan.Debt)
            {
                message = "Informe um pagamento válido até o saldo da dívida.";
                return false;
            }

            if (state.Gold < amount)
            {
                message = "O tesouro não tem ouro suficiente.";
                return false;
            }

            state.Gold -= amount;
            state.Loan.Paid += amount;
            state.Loan.Debt -= amount;
            if (state.Loan.Debt == 0)
            {
                state.Loan = new LoanRecord();
                message = "Empréstimo quitado.";
            }
            else
            {
                message = "Pagamento registrado. Dívida restante: " + state.Loan.Debt + " ouros.";
            }

            return true;
        }

        public static bool TryAdjustGold(VillageState state, int amount, out string message)
        {
            message = string.Empty;
            if (amount == 0 || state.Gold + amount < 0 || Math.Abs((long)amount) > 1000000)
            {
                message = "Informe um ajuste de até 1.000.000 ouros sem deixar saldo negativo.";
                return false;
            }

            state.Gold += amount;
            state.AdjustedGold += amount;
            message = amount > 0 ? "Ouro adicionado ao tesouro." : "Ouro removido do tesouro.";
            return true;
        }

        public static void AdvanceMinute(VillageState state)
        {
            if (state.Loan.Debt <= 0)
            {
                return;
            }

            state.Loan.ElapsedHours += 1.0 / 60.0;
            double nextInterestHour = LoanTermHours + InterestIntervalHours * (state.Loan.InterestDays + 1);

            if (state.Loan.ElapsedHours + 1e-8 < nextInterestHour)
            {
                return;
            }

            int interest = (int)Math.Ceiling(state.Loan.Debt * 0.01);
            state.Loan.Interest += interest;
            state.Loan.Debt += interest;
            state.Loan.InterestDays++;
        }

        private static int GetStock(ResourceStock stock, ResourceType type)
        {
            switch (type)
            {
                case ResourceType.Timber: return stock.Timber;
                case ResourceType.Stone: return stock.Stone;
                case ResourceType.Iron: return stock.Iron;
                case ResourceType.Food: return stock.Food;
                default: return 0;
            }
        }

        private static void SetStock(ResourceStock stock, ResourceType type, int value)
        {
            switch (type)
            {
                case ResourceType.Timber: stock.Timber = value; break;
                case ResourceType.Stone: stock.Stone = value; break;
                case ResourceType.Iron: stock.Iron = value; break;
                case ResourceType.Food: stock.Food = value; break;
            }
        }
    }
}
