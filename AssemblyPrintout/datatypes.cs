using System;
using System.Linq;
using System.Collections.Generic;

namespace AssemblyPrintout
{
    static class Datatypes
    {

        #region Jobber
        public class BackorderedItem
        {
            public int ItemNumber { get; set; }
            public string ItemName { get; set; }
            public int QuantityOnBackOrder { get; set; }
            public int QuantityAllocatable { get; set; }
            public int InvoiceNumber { get; set; }

            public BackorderedItem() { }

            public BackorderedItem(int InvoiceNumber, string[] Data)
            {
                this.InvoiceNumber = InvoiceNumber;
                this.QuantityOnBackOrder = int.TryParse(Data[0], out int Quantity) ? Quantity : -1;
                this.ItemNumber = int.TryParse(Data[1], out int ItemNumber) ? ItemNumber : -1;
                this.ItemName = Data[2];
            }

            public BackorderedItem(int ItemNumber, int Quantity, int QuantityAllocatable)
            {
                this.ItemNumber = ItemNumber;
                this.QuantityOnBackOrder = Quantity;
                this.QuantityAllocatable = QuantityAllocatable;
            }

            public void Update(int Quantity, int QuantityAllocatable)
            {
                this.QuantityOnBackOrder += Quantity;
                this.QuantityAllocatable += QuantityAllocatable;
            }

            public override string ToString() => $"Item: {ItemNumber + 90000} │ Qty: {Util.PadL(7, this.QuantityOnBackOrder.ToString())}";


        }
        public class BackorderedItemExportHelper
        {
            public Customer C { get; set; }
            public Invoice I { get; set; }
            public BackorderedItem B { get; set; }
            public BackorderedItemExportHelper() { }
            public BackorderedItemExportHelper(Customer C, Invoice I, BackorderedItem B)
            {
                this.C = C;
                this.I = I;
                this.B = B;
            }

            public override string ToString() => $"Customer: {Util.PadR(6, C.CustomerCode)} │ Invoice #: {Util.PadR(6, B.InvoiceNumber.ToString())} │ PO #: {Util.PadR(12, I.PurchaseOrderNumber)} │ Date: {Util.PadR(10, I.OrderDateAsString)} │ Qty: {Util.PadL(7, B.QuantityOnBackOrder.ToString())}";
        }

        public class InvoiceItem
        {
            public string ItemNumber { get; set; }
            public string Description { get; set; }

            public int QuantityOrdered { get; set; }
            public int QuantityBackOrdered { get; set; }
        }
        public class Invoice
        {
            public int InvoiceNumber { get; set; }

            public string PurchaseOrderNumber { get; set; }

            public string OrderDateRaw { get; set; }

            public DateTime? OrderDate { get; set; }

            public string InvoiceFilePath { get; set; }

            public List<InvoiceItem> Items { get; set; }
            public List<BackorderedItem> BackorderedItems { get; set; }

            public decimal Discount { get; set; }

            public Invoice()
            {
                Items = new List<InvoiceItem>();
                BackorderedItems = new List<BackorderedItem>();
            }
            public Invoice(int InvoiceNumber)
            {
                Items = new List<InvoiceItem>();
                BackorderedItems = new List<BackorderedItem>();
                this.InvoiceNumber = InvoiceNumber;
            }

            public string OrderDateAsString => (!OrderDate.HasValue) ? "00/00/00" : $"{((this.OrderDate.Value.Month < 10) ? $"0{this.OrderDate.Value.Month}" : $"{this.OrderDate.Value.Month}")}/{((this.OrderDate.Value.Day < 10) ? $"0{this.OrderDate.Value.Day}" : $"{this.OrderDate.Value.Day}")}/{((this.OrderDate.Value.Year < 10) ? $"0{this.OrderDate.Value.Year}" : $"{this.OrderDate.Value.Year}")}";

        }
        public class Customer
        {
            public string CustomerId { get; set; }

            public string CustomerCode { get; set; }

            public List<string> CustomerInfo { get; set; }

            //public string CustomerName { get; set; }
            //public string Address { get; set; }
            //public string Address2 { get; set; }
            //public string Address3 { get; set; }

            public Dictionary<int, Invoice> Invoices { get; set; }

            public Customer()
            {
                Invoices = new Dictionary<int, Invoice>();
                CustomerInfo = new List<string>();
            }

            public Customer(string CustomerId, string CustomerCode)
            {
                Invoices = new Dictionary<int, Invoice>();
                CustomerInfo = new List<string>();
                this.CustomerId = CustomerId;
                this.CustomerCode = CustomerCode;
            }

            private static List<Invoice> ExtractedInvoices = new List<Invoice>();

            public static List<Invoice> ExtractInvoices(IEnumerable<Customer> Customers) => Extract(Customers);

            private static List<Invoice> Extract(IEnumerable<Customer> Cs)
            {
                Cs.ToList().ForEach(x => { ExtractedInvoices.AddRange(x.Invoices.Values); });
                return ExtractedInvoices;
            }
        }
        public class ProductAllocationModel
        {
            public Dictionary<int, ProductAllocationItem> AllocatedItems { get; set; }

            public ProductAllocationModel() { AllocatedItems = new Dictionary<int, ProductAllocationItem>(); }

            public void AddProducts(List<ProductModel> Products) => Products.ForEach(x => { if (!AllocatedItems.ContainsKey(x.Number)) AllocatedItems.Add(x.Number, new ProductAllocationItem(x)); });

            public bool HasProduct(int ProductNumber) => this.AllocatedItems.ContainsKey((ProductNumber < 90000 ? ProductNumber + 90000 : ProductNumber));

            public void TotalProductBackorders(List<Invoice> Invoices)
            {
                foreach (var item in Invoices.Select(x => x.BackorderedItems).ToList())
                {
                    item.ForEach(iitem =>
                    {
                        this.AllocatedItems[iitem.ItemNumber + 90000].QuantityOnBackOrder += iitem.QuantityOnBackOrder;
                    });
                }
            }

            public int TotalBackorderedItems => AllocatedItems.Any() ? AllocatedItems.Values.Sum(x => x.QuantityOnBackOrder) : 0;

            public double TotalValueOfBackOrders => AllocatedItems.Sum(x => x.Value.DollarValue);

            public List<Invoice> AllocateInvoices(List<Invoice> Invoices, bool SortByInvoiceNumber = true)
            {
                if (Invoices != null && Invoices.Any())
                    (SortByInvoiceNumber ? Invoices.OrderBy(x => x.InvoiceNumber).ToList() : Invoices).ForEach(item =>
                    {
                        item.BackorderedItems.ForEach(item1 =>
                        {
                            if (AllocatedItems.ContainsKey(ProductNumber(item1.ItemNumber)))
                            {
                                item1.QuantityAllocatable = item1.QuantityOnBackOrder < AllocatedItems[ProductNumber(item1.ItemNumber)].QuantityAvailable ? item1.QuantityOnBackOrder : AllocatedItems[ProductNumber(item1.ItemNumber)].QuantityAvailable;
                                if (!AllocatedItems.ContainsKey(ProductAllocationModel.ProductNumber(item1.ItemNumber))) AllocatedItems.Add(ProductAllocationModel.ProductNumber(item1.ItemNumber), new ProductAllocationItem());
                                AllocatedItems[ProductAllocationModel.ProductNumber(item1.ItemNumber)].AllocateOrder(item.InvoiceNumber, item1.QuantityAllocatable);
                            }
                        });
                    });

                return Invoices;
            }

            public int Allocate(BackorderedItem item)
            {
                if (this.HasProduct(item.ItemNumber))
                {
                    item.QuantityAllocatable = item.QuantityOnBackOrder < AllocatedItems[ProductNumber(item.ItemNumber)].QuantityAvailable ? item.QuantityOnBackOrder : AllocatedItems[ProductNumber(item.ItemNumber)].QuantityAvailable;
                    if (!AllocatedItems.ContainsKey(ProductAllocationModel.ProductNumber(item.ItemNumber))) AllocatedItems.Add(ProductAllocationModel.ProductNumber(item.ItemNumber), new ProductAllocationItem());
                    AllocatedItems[ProductAllocationModel.ProductNumber(item.ItemNumber)].AllocateOrder(item.InvoiceNumber, item.QuantityAllocatable);
                    return item.QuantityAllocatable;
                }
                return 0;
            }

            public static int ProductNumber(int Number) => Number > 90000 ? Number : Number + 90000;

            public static int ProductId(int Number) => Number > 90000 ? Number : Number + 90000;

            //public void AllocateProducts(List<InvoiceModel> Invoices)
            //{
            //    foreach (var item in Invoices)
            //    {
            //        item.BackorderedItems.ForEach(iitem =>
            //        {
            //            this.AllocatedItems[ProductNumber(iitem.ItemNumber)].QuantityOnBackorder += iitem.QuantityOnOrder;
            //            this.AllocatedItems[ProductNumber(iitem.ItemNumber)].QuantityOnHand += iitem.QuantityOnOrder;
            //        });
            //    }
            //}

            //public void AllocateProducts(List<InvoiceModel> Invoices, DateTime DateLimit)
            //{
            //    foreach (var item in Invoices)
            //    {
            //        item.BackorderedItems.ForEach(iitem =>
            //        {
            //            if (item.OrderDate <= DateLimit) this.AllocatedItems[ProductNumber(iitem.ItemNumber)].QuantityOnBackorder += iitem.QuantityOnOrder;
            //            this.AllocatedItems[ProductNumber(iitem.ItemNumber)].QuantityOnHand += iitem.QuantityOnOrder;
            //        });
            //    }
            //}

        }
        public class ProductAllocationItem
        {
            public int ProductNumber { get; set; }
            public ProductModel Product { get; set; }
            public int QuantityOnHand { get; set; }
            public int QuantityOnBackOrder { get; set; }
            public Dictionary<int, int> Allocation { get; set; }

            public ProductAllocationItem()
            {
                Allocation = new Dictionary<int, int>();
            }
            public ProductAllocationItem(int ProductNumber, int QuantityOnHand)
            {
                this.ProductNumber = ProductNumber;
                this.QuantityOnHand = QuantityOnHand;
                Allocation = new Dictionary<int, int>();
            }
            public ProductAllocationItem(ProductModel Product)
            {
                this.Product = Product;
                this.ProductNumber = Product.Number;
                this.QuantityOnHand = Product.QuantityOnHand;
                Allocation = new Dictionary<int, int>();
            }

            public double DollarValue => QuantityOnBackOrder * Product.ListPrice;

            public void AllocateOrder(int InvoiceNumber, int Quantity)
            {
                if (!this.Allocation.ContainsKey(InvoiceNumber)) this.Allocation.Add(InvoiceNumber, Quantity);
                else this.Allocation[InvoiceNumber] += Quantity;
            }

            public int AllocatedOnOrder(int InvoiceNumber) => Allocation.ContainsKey(InvoiceNumber) ? Allocation[InvoiceNumber] : 0;

            public int ActualOnHand => QuantityOnHand + QuantityOnBackOrder;

            public int Allocated => (Allocation.Any() ? Allocation.Sum(x => x.Value) : 0);

            public int QuantityAvailable => Allocated < ActualOnHand ? ActualOnHand - Allocated : 0;

        }
        #endregion
        #region Global Models
        public class ProductModel
        {
            public int Id { get; set; }
            public int Number { get; set; }
            public string Description { get; set; }
            public double ListPrice { get; set; }
            public double CostOfProduction { get; set; }
            public double Weight { get; set; }
            public double MasterUnits { get; set; }
            public double CubicFeet { get; set; }
            public int QuantityOnHand { get; set; }
            public int AnnualUse { get; set; }
            public int SalesLastPeriod { get; set; }
            public int YTDSales { get; set; }
            public int Gross { get; set; }
            public int AssemblyTime { get; set; }
            public int ProductCode { get; set; }
            public string RequiredPartsData { get; set; }
            //public double Needed { get; set; }
            public ProductSubModel ExtraData { get; set; }
            public Dictionary<int, int> RequiredParts { get; set; }
            public Dictionary<int, int> RequiredProducts { get; set; }
            public Dictionary<int, int> AssemblyKits { get; set; }

            public ProductModel()
            {
                ExtraData = new ProductSubModel();
                RequiredParts = new Dictionary<int, int>();
                RequiredProducts = new Dictionary<int, int>();
                AssemblyKits = new Dictionary<int, int>();
            }

            public ProductModel(int Number, string Description, double ListPrice, double Weight, double MasterUnits, double CubicFeet, int QuantityOnHand, int AnnualUse, int SalesLastPeriod, int YTDSales, int Gross, int AssemblyTime, int ProductCode)
            {
                ExtraData = new ProductSubModel();
                RequiredParts = new Dictionary<int, int>();
                RequiredProducts = new Dictionary<int, int>();
                AssemblyKits = new Dictionary<int, int>();

                this.Number = Number;
                this.Description = Description;
                this.ListPrice = ListPrice;
                this.Weight = Weight;
                this.MasterUnits = MasterUnits;
                this.CubicFeet = CubicFeet;
                this.QuantityOnHand = QuantityOnHand;
                this.AnnualUse = AnnualUse;
                this.SalesLastPeriod = SalesLastPeriod;
                this.YTDSales = YTDSales;
                this.Gross = Gross;
                this.AssemblyTime = AssemblyTime;
                this.ProductCode = ProductCode;
            }
            public void AddRequiredItem(string Item, string Quantity)
            {
                if (!string.IsNullOrEmpty(Item.Trim()) && int.TryParse(Item, out int ItemId) && ItemId > 0)
                {
                    if (int.TryParse(Quantity, out int QuantityValue))
                    {
                        if (this.RequiredParts.ContainsKey(ItemId))
                        {
                            this.RequiredParts[ItemId] += QuantityValue;
                        }
                        else
                        {
                            this.RequiredParts.Add(ItemId, QuantityValue);
                        }
                    }
                    else if (Quantity == "P") { this.RequiredProducts.Add(ItemId, 1); }
                }
            }

            public ProductModel PopulateProduct(Dictionary<int, ProductModel> Products, Dictionary<int, AssemblyKitModel> AssemblyKits)
            {
                this.RequiredProducts.ToList().ForEach(item =>
                {
                    if (Products.ContainsKey(item.Key))
                        Products[item.Key].RequiredParts.ToList().ForEach(item1 =>
                        {
                            if (this.RequiredParts.ContainsKey(item1.Key)) this.RequiredParts[item1.Key] += item1.Value;
                            else this.RequiredParts.Add(item1.Key, item1.Value);
                        });
                });
                List<int> sad = this.AssemblyKits.Keys.ToList();
                for (int i = 0; i < sad.Count(); i++)
                {
                    if (AssemblyKits.ContainsKey(sad[i]))
                    {
                        AssemblyKits[sad[i]].RequiredParts.ToList().ForEach(item1 =>
                        {
                            if (this.RequiredParts.ContainsKey(item1.Key)) this.RequiredParts[item1.Key] += item1.Value;
                            else this.RequiredParts.Add(item1.Key, item1.Value);
                        });
                        if (AssemblyKits[sad[i]].AttachedAssemblyKitId != null && AssemblyKits[sad[i]].AttachedAssemblyKitId > 0) sad.Add((int)AssemblyKits[sad[i]].AttachedAssemblyKitId);
                    }
                }
                return this;
            }
        }
        public class PartModel
        {
            public int Id { get; set; }

            public int PartNumber { get; set; }

            public string Vendor { get; set; }

            public string PartTypePrefix { get; set; }

            public string PartDescription { get; set; }

            public string Specification { get; set; }

            public string SpecialInstructionIds { get; set; }
            public List<int> GenericSpecIds { get; set; }
            public Dictionary<int, string> GenericSpecData { get; set; }

            public int LeadTimeWeeks { get; set; }

            public double FinishedWeight { get; set; }

            public double CostOfProduction { get; set; }

            public double SalePrice { get; set; }

            public int BestQuantityToOrder { get; set; }

            public double YearsUse { get; set; }

            public int LastYearsUse { get; set; }

            public decimal QuantityOnHand { get; set; }

            public int YearToDateSales { get; set; }

            public decimal QuantityInProducts { get; set; }

            public int WeeksCushion { get; set; }

            public int GlobalCycleTime { get; set; }
            public int SecondCycleTime { get; set; }

            public int MachineNumberOne { get; set; }
            public int MachineNumberTwo { get; set; }
            //public Dictionary<FileType, List<Tuple<int, int>>> Files { get; set; }
            //public string FileData { get; set; }

            //[DefaultValue("[]")]
            //[StringLength(maximumLength: 500, MinimumLength = 2)]
            public string PKAData { get; set; }
            public List<string> PreviousKnownAs { get; set; }

            public PartModel()
            {
                PartDescription = "";
                Specification = "";
                SpecialInstructionIds = "[]";
                PKAData = "[]";
                LeadTimeWeeks = 0;
                FinishedWeight = 0;
                CostOfProduction = 0;
                SalePrice = 0;
                BestQuantityToOrder = 0;
                YearsUse = 0;
                LastYearsUse = 0;
                QuantityOnHand = 0;
                YearToDateSales = 0;
                QuantityInProducts = 0;
                MachineNumberOne = 0;
                MachineNumberTwo = 0;
                WeeksCushion = 0;
                GlobalCycleTime = 0;
                SecondCycleTime = 0;
                PreviousKnownAs = new List<string>();
                GenericSpecData = new Dictionary<int, string>();
                GenericSpecIds = new List<int>();
            }

            public PartModel(int PartNumber)
            {
                this.PartNumber = PartNumber;
                PartDescription = "";
                Specification = "";
                SpecialInstructionIds = "[]";
                PKAData = "[]";
                LeadTimeWeeks = 0;
                FinishedWeight = 0;
                CostOfProduction = 0;
                SalePrice = 0;
                BestQuantityToOrder = 0;
                YearsUse = 0;
                LastYearsUse = 0;
                QuantityOnHand = 0;
                YearToDateSales = 0;
                QuantityInProducts = 0;
                GlobalCycleTime = 0;
                SecondCycleTime = 0;
                WeeksCushion = 0;
                PreviousKnownAs = new List<string>();
                GenericSpecData = new Dictionary<int, string>();
                GenericSpecIds = new List<int>();
            }
            public PartModel(string[] P)
            {
                if (P == null || P.Length < 36) return;
                this.PartNumber = (!string.IsNullOrEmpty(P[0]) && P[0].ToString().Trim().Length == 6 && int.TryParse(P[0].Substring(2, 4), out int PartNumber) ? PartNumber : 0);
                this.PartTypePrefix = P[0].Substring(0, 2);
                #region all the shit for parts
                this.PartDescription = (!string.IsNullOrEmpty(P[1])) ? P[1] : "";
                this.Specification = (!string.IsNullOrEmpty(P[2])) ? P[2] : "";
                //this.SpecialInstructionId = int.TryParse(P[3], out int SpecInstrId) ? SpecInstrId : 0;
                this.YearsUse = (int.TryParse(P[4], out int yearuse)) ? yearuse : 0;
                this.LeadTimeWeeks = (int.TryParse(P[5], out int leadtime)) ? leadtime : 0;
                this.Vendor = P[6].Trim();
                this.BestQuantityToOrder = (int.TryParse(P[7], out int orderquan)) ? orderquan : 0;
                this.FinishedWeight = (double.TryParse(P[8], out double finWeight)) ? (finWeight > 0) ? (finWeight / 10000) : 0 : 0;
                this.CostOfProduction = (double.TryParse(P[9], out double cost)) ? cost : 0;

                this.QuantityOnHand = (decimal.TryParse(P[19], out decimal onhand)) ? onhand : 0;
                this.YearToDateSales = (int.TryParse(P[22], out int ytd)) ? ytd : 0;
                //thisPart.LatestQuote = (double.TryParse(P[23], out double quote)) ? quote : 0;
                this.QuantityInProducts = (decimal.TryParse(P[24], out decimal assembled)) ? assembled : 0;
                this.WeeksCushion = (int.TryParse(P[28], out int WksCush)) ? WksCush : 0;

                this.GlobalCycleTime = (int.TryParse(P[25], out int _Cycle)) ? _Cycle : GlobalCycleTime;

                this.MachineNumberOne = (int.TryParse(P[26], out int MandR1) && MandR1 > 0) ? (int)(MandR1 * 0.01) : this.MachineNumberOne;
                this.MachineNumberTwo = (int.TryParse(P[17], out int MandR2) && MandR2 > 0) ? MandR2 - ((int)(MandR2 * 0.01) * 100) : this.MachineNumberTwo;
                this.SecondCycleTime = (int.TryParse(P[17], out int _SecondCycle) && _SecondCycle > 0) ? (int)(_SecondCycle * 0.01) : GlobalCycleTime;

                this.LastYearsUse = (int.TryParse(P[27], out int lastyear)) ? lastyear : 0;
                this.SalePrice = (double.TryParse(P[32], out double saleprice)) ? (saleprice > 0) ? (saleprice / 100) : 0 : 0;

                #endregion all the shit for parts
            }
        }
        public class StringAlongModel
        {
            public int Number { get; set; }
            public string Name { get; set; }
            public int AttachedStringAlong { get; set; }
            public Dictionary<int, int> Items { get; set; }
            public StringAlongModel() { Items = new Dictionary<int, int>(); }
            public StringAlongModel(int Number, string[] Data)
            {
                if (Data.Length != 42) return;
                Items = new Dictionary<int, int>();
                this.Number = Number;
                this.Name = Data[0];
                for (int i = 1; i < 40; i += 2)
                    if (int.TryParse(Data[i], out int ItemId) && ItemId > 0 && int.TryParse(Data[i + 1], out int Quantity))
                        if (!Items.ContainsKey(ItemId)) Items.Add(ItemId, Quantity);
                        else Items[ItemId] += Quantity;

                if (int.TryParse(Data[41], out int AttachedStringAlong)) this.AttachedStringAlong = AttachedStringAlong;
            }
        }
        #endregion
        #region OrganizeBackorder Models
        public class Entry
        {
            public string ProductNum { get; set; }
            public string Description { get; set; }
            public string ProductCode { get; set; }
            public string AssemblyTime { get; set; }
            public string Qty { get; set; }
            public List<Part> Parts { get; set; }

            public Entry()
            {
                Parts = new List<Part>();
            }

            public Entry(string NewData)
            {
                Parts = new List<Part>();

            }
        }
        public class Part
        {
            public string PartId { get; set; }
            public string Description { get; set; }
            public string Vendor { get; set; }
        }
        public class ProductData
        {
            public List<string> Data { get; set; }
            public List<PartData> Parts { get; set; }
            public ProductData()
            {
                Parts = new List<PartData>();
            }
        }
        public class PartData
        {
            public List<string> Data { get; set; }
            public PartData()
            {
                Data = new List<string>();
            }
            public PartData(IEnumerable<string> Data)
            {
                this.Data = Data.ToList();
            }
        }
        public class AssemblyKitModel
        {
            public int Id { get; set; }

            public int AssemblyKitNumber { get; set; }

            public string Name { get; set; }

            public string Description { get; set; }

            public int? AttachedAssemblyKitId { get; set; }
            public AssemblyKitModel AttachedAssemblyKit { get; set; }

            public string RequiredPartsData { get; set; }
            public Dictionary<int, int> RequiredParts { get; set; }

            public string RequiredProductsData { get; set; }
            public Dictionary<int, int> RequiredProducts { get; set; }

            public AssemblyKitModel()
            {
                RequiredPartsData = "{}"; RequiredProductsData = "{}";
                RequiredParts = new Dictionary<int, int>(); RequiredProducts = new Dictionary<int, int>();

            }

            public AssemblyKitModel(int AssemblyKitNumber, string Name, string Description, int? AttachedAssemblyKitId, Dictionary<int, int> RequiredParts, Dictionary<int, int> RequiredProducts)
            {
                this.AssemblyKitNumber = AssemblyKitNumber;
                this.Name = Name;
                this.Description = Description;
                this.AttachedAssemblyKitId = AttachedAssemblyKitId;
                this.RequiredPartsData = "{}"; this.RequiredProductsData = "{}";
                this.RequiredParts = RequiredParts ?? new Dictionary<int, int>();
                this.RequiredProducts = RequiredProducts ?? new Dictionary<int, int>();
            }
        }
        #endregion
        public class InitData
        {
            public double TodaysProduction { get; set; }
            public double YesterdaysProduction { get; set; }
            public double ProductionRequired { get; set; }
            public double LastYearDailyAvgByMonth { get; set; }
        }
        public class PartValues
        {
            public decimal YearsUse { get; set; }
            public decimal OnHand { get; set; }
            public decimal QuantityAllocated { get; set; }
        }
        public class ProductionItem
        {
            public int ProductNumber { get; set; }
            public int NumberAssembled { get; set; }

            public ProductionItem() { }
            public ProductionItem(string ProdBakLine)
            {
                if (string.IsNullOrEmpty(ProdBakLine) || ProdBakLine.Length < 60) return;
                this.ProductNumber = int.TryParse(ProdBakLine.Substring(0, 5), out int Num) ? Num : 0;
                this.NumberAssembled = int.TryParse(ProdBakLine.Substring(49, 8), out int Ass) ? Ass : 0;
            }

        }
        public class neededAndAnnual
        {
            public double AnnualHours { get; set; }
            public double Needed30 { get; set; }
            public double Needed60 { get; set; }
        }
        public enum ErrorType
        {
            CSharpError, JobberError, QBError
        }

        public class DatasetRAW
        {
            public List<ProductCode> ProductCodes { get; set; }
            public double AssembledHours { get; set; }
            public double XdaysSupply { get; set; }
            public Daily7Data daily7Data { get; set; }
            public double AnnualAssemblyHours { get; set; }
            public double ProdSurplusHr30 { get; set; }
            public double ProdSurplusHr60 { get; set; }
            public double ProdSurplusHr90 { get; set; }
            public double ProdHrNeedThirty { get; set; }
            public double ProdHrNeedSixty { get; set; }
            public double ProdHrNeedNinety { get; set; }
            public double YesterdaysProductionHours { get; set; }

            public DatasetRAW()
            {
                ProductCodes = new List<ProductCode>();
            }
        }
        public class ProductCode
        {
            public int Productcode { get; set; }

            public double TotalNeeded { get; set; }

            //hours to make 30 day supply
            public double XdaysSupply { get; set; }

            public double HoursAssembled { get; set; }
            public List<ProductModel> Products { get; set; }
            public int DayLimit { get; set; }
            public double AssemblyHours { get; set; }
            public ProductCode()
            {
                Products = new List<ProductModel>();
            }
            public ProductCode(string Line)
            {
                Line = Line.Split('╝').Last();
                string[] NewCode = Line.Split(',');
                if (NewCode.Length == 3)
                {
                    this.Productcode = int.TryParse(NewCode[0], out int CodeVal) ? CodeVal : 0;
                    this.HoursAssembled = double.TryParse(NewCode[1], out double HoursAssembled) ? HoursAssembled : 0;
                    this.DayLimit = int.TryParse(NewCode[2], out int DayLimit) ? DayLimit : 0;
                }
            }
            public ProductCode InitCode(List<ProductModel> ProductsIn)
            {
                ProductsIn.ForEach(item =>
                {
                    double AnnualUseMath = ((item.AnnualUse * Util.Inv365) * this.DayLimit) - item.QuantityOnHand;
                    double YTDMath = ((item.YTDSales / Util.Getj30) * this.DayLimit) - item.QuantityOnHand;
                    item.ExtraData.DaysSupply = item.QuantityOnHand != 0 && item.AnnualUse != 0 ? ((double)((double)item.QuantityOnHand / (double)item.AnnualUse) * 365) : 0;
                    item.ExtraData.Needed = (Util.Getj30 > 60 && AnnualUseMath < YTDMath) ? YTDMath : AnnualUseMath;
                    if (item.ExtraData.DaysSupply <= this.DayLimit)
                    {
                        item.ExtraData.NeededAssemblyHours = (item.ExtraData.Needed * item.AssemblyTime) / 3600;
                        if (item.ExtraData.NeededAssemblyHours < 1) { item.ExtraData.NeededAssemblyHours = Math.Round(item.ExtraData.NeededAssemblyHours, 3, MidpointRounding.AwayFromZero); }
                        else { item.ExtraData.NeededAssemblyHours = Math.Round(item.ExtraData.NeededAssemblyHours, 2, MidpointRounding.AwayFromZero); }
                        var PartList = new List<PartSubModel>();
                        foreach (KeyValuePair<int, int> Part in item.RequiredParts)
                        {
                            if (Util.PartDictionary.ContainsKey(Part.Key))
                            {
                                var tempPart = new PartSubModel(Util.PartDictionary[Part.Key]);
                                if (tempPart.Part.QuantityOnHand == 0 || tempPart.Part.YearsUse == 0) tempPart.ds = 0; else tempPart.ds = Math.Round(((decimal)tempPart.Part.QuantityOnHand / (decimal)tempPart.Part.YearsUse) * 365, 0, MidpointRounding.AwayFromZero);
                                if (!Util.PartPrefixFilter.Contains(tempPart.Part.PartTypePrefix) && !Util.PartNumberFilter.Contains(tempPart.PartNumber)) PartList.Add(tempPart);
                            }
                        }
                        foreach (int ProductId in item.RequiredProducts.Keys)
                        {
                            if (Util.ProductDictionary.ContainsKey(ProductId))
                            {
                                var Product = Util.ProductDictionary[ProductId];
                                foreach (KeyValuePair<int, int> Part in Product.RequiredParts)
                                {
                                    if (Util.PartDictionary.ContainsKey(Part.Key))
                                    {
                                        var tempPart = new PartSubModel(Util.PartDictionary[Part.Key]);
                                        if (tempPart.Part.QuantityOnHand == 0 || tempPart.Part.YearsUse == 0) tempPart.ds = 0; else tempPart.ds = Math.Round(((decimal)tempPart.Part.QuantityOnHand / (decimal)tempPart.Part.YearsUse) * 365, 0, MidpointRounding.AwayFromZero);
                                        if (!Util.PartPrefixFilter.Contains(tempPart.Part.PartTypePrefix) && !Util.PartNumberFilter.Contains(tempPart.PartNumber)) PartList.Add(tempPart);
                                    }
                                }
                            }
                        }
                        foreach (int KitId in item.AssemblyKits.Keys)
                        {
                            if (Util.KitDictionary.ContainsKey(KitId))
                            {
                                var Kit = Util.KitDictionary[KitId];
                                foreach (var Part in Kit.RequiredParts)
                                {
                                    if (Util.PartDictionary.ContainsKey(Part.Key))
                                    {
                                        var tempPart = new PartSubModel(Util.PartDictionary[Part.Key]);
                                        if (tempPart.Part.QuantityOnHand == 0 || tempPart.Part.YearsUse == 0) tempPart.ds = 0; else tempPart.ds = Math.Round(((decimal)tempPart.Part.QuantityOnHand / (decimal)tempPart.Part.YearsUse) * 365, 0, MidpointRounding.AwayFromZero);
                                        if (!Util.PartPrefixFilter.Contains(tempPart.Part.PartTypePrefix) && !Util.PartNumberFilter.Contains(tempPart.PartNumber)) PartList.Add(tempPart);
                                    }
                                }
                            }
                        }
                        if (PartList.Any())
                        {
                            item.ExtraData.LowPart = PartList.OrderBy(x => x.ds).ToList().FirstOrDefault();
                            item.ExtraData.DoNotExceed = Math.Round(((item.AnnualUse / 365) * item.ExtraData.LowPart.ds) - item.QuantityOnHand, 0, MidpointRounding.AwayFromZero);
                        }
                        else { item.ExtraData.DoNotExceed = 0; }
                    }
                });
                this.XdaysSupply = Math.Round(ProductsIn.Select(x => x.ExtraData).Sum(x => x.NeededAssemblyHours), 1, MidpointRounding.AwayFromZero);
                this.TotalNeeded = Math.Round(ProductsIn.Select(x => x.ExtraData).Sum(x => x.Needed), 0, MidpointRounding.AwayFromZero);
                this.Products = ProductsIn.Where(x => x.ExtraData.DaysSupply < DayLimit).OrderBy(x => x.ExtraData.DaysSupply).ToList();
                return this;
            }
        }
        public class ProductSubModel
        {
            //days supply
            public double DaysSupply { get; set; }
            //needed
            public double Needed { get; set; }
            //potential for assembly
            public decimal DoNotExceed { get; set; }
            public PartSubModel LowPart { get; set; }
            public double NeededAssemblyHours { get; set; }
            public double AnnualAssemblyHours { get; set; }
            public ProductSubModel() { }
        }
        public class PartSubModel
        {
            public int PartNumber { get; set; }

            public PartModel Part { get; set; }

            //quantity needed for assembly
            //public decimal qn { get; set; }

            //estimated dats supply
            public decimal ds { get; set; }
            public PartSubModel() { }
            public PartSubModel(PartModel Part)
            {
                this.Part = Part;
                this.PartNumber = Part.PartNumber;
            }
        }
        public class PrintProduct
        {
            public string prod { get; set; }
            public string desc { get; set; }
            public string yu { get; set; }
            public string ds { get; set; }
            public string oh { get; set; }
            public string dne { get; set; }
            public string part { get; set; }
            public string need { get; set; }
            public string hour { get; set; }
        }
        public class Daily7Data
        {
            public List<string> partNumbers { get; set; }
            public string hoursForYearsSales { get; set; }
            public string prodHoursPerDay { get; set; }
            public string totalHours { get; set; }
            public string assembledHours { get; set; }
            public string hoursNeeded30 { get; set; }
            public string surplusHours30 { get; set; }
            public string hoursNeeded60 { get; set; }
            public string surplusHours60 { get; set; }
            public string hoursNeeded90 { get; set; }
            public string surplusHours90 { get; set; }
        }

        public class ProductionDataPack
        {
            public List<ProductionLine> Today { get; set; }
            public ProductionDataPack()
            {
                Today = new List<ProductionLine>();
            }
            //public List<productionLine> yesterday { get; set; }
        }
        public class ProductionLine
        {
            public int Produced { get; set; }
            public decimal AssemblyTime { get; set; }
            public ProductionLine() { }
            public ProductionLine(int Produced, decimal AssemblyTime)
            {
                this.Produced = Produced;
                this.AssemblyTime = AssemblyTime;
            }
        }

        public class CheckPart
        {
            public int? DayLimit { get; set; }
            public List<CheckPartPart> Parts { get; set; }
            public CheckPart()
            {
                Parts = new List<CheckPartPart>();
            }
        }

        public class CheckPartPart
        {
            public string Part { get; set; }
            public string Desc { get; set; }
            public int Qty { get; set; }
            public string Vend { get; set; }
        }

    }
}
