// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using WideWorldImporters.Shared.ApiSdk;
using WideWorldImporters.Shared.ApiSdk.Extensions;
using WideWorldImporters.Shared.ApiSdk.Models.WideWorldImportersService;
using WideWorldImporters.Shared.Infrastructure;
using WideWorldImporters.Shared.Models;
using WideWorldImporters.Web.Client.Infrastructure;
using WideWorldImporters.Web.Client.Shared.Infrastructure;

namespace WideWorldImporters.Web.Client.Pages
{
    public partial class InvoicesDataGrid
    {
        private GridItemsProvider<Invoice> InvoiceProvider = default!;

        private FluentDataGrid<Invoice> DataGrid = default!;

        private readonly PaginationState Pagination = new() { ItemsPerPage = 10 };

        private readonly FilterState FilterState = new();

        private readonly EventCallbackSubscriber<FilterState> CurrentFiltersChanged;

        public InvoicesDataGrid()
        {
            CurrentFiltersChanged = new(EventCallback.Factory.Create<FilterState>(this, RefreshData));
        }

        protected override Task OnInitializedAsync()
        {
            InvoiceProvider = async request =>
            {
                var response = await GetInvoices(request);

                if (response == null || response.Value == null)
                {
                    return GridItemsProviderResult.From(items: new List<Invoice>(), totalItemCount: 0);
                }

                return GridItemsProviderResult.From(
                    items: response.Value,
                    totalItemCount: response.GetODataCount());
            };

            return base.OnInitializedAsync();
        }

        protected override Task OnParametersSetAsync()
        {
            CurrentFiltersChanged.SubscribeOrMove(FilterState.CurrentFiltersChanged);

            return Task.CompletedTask;
        }

        private Task RefreshData()
        {
            return DataGrid.RefreshDataAsync();
        }

        private async Task<InvoiceCollectionResponse?> GetInvoices(GridItemsProviderRequest<Invoice> request)
        {
            var sortColumns = DataGridUtils.GetSortColumns(request);
            var filters = FilterState.Filters.Values.ToList();

            var parameters = ODataQueryParameters.Builder
                .SetPage(Pagination.CurrentPageIndex + 1, Pagination.ItemsPerPage)
                .SetFilter(filters)
                .AddExpand(nameof(Invoice.LastEditedByNavigation))
                .AddOrderBy(sortColumns)
                .Build();

            return await ApiClient.Odata.Invoices.GetAsync(request =>
            {
                request.QueryParameters.Count = true;
                request.QueryParameters.Top = parameters.Top;
                request.QueryParameters.Skip = parameters.Skip;

                if (parameters.Expand != null)
                {
                    request.QueryParameters.Expand = parameters.Expand;
                }

                if (!string.IsNullOrWhiteSpace(parameters.Filter))
                {
                    request.QueryParameters.Filter = parameters.Filter;
                }

                if (parameters.OrderBy != null)
                {
                    request.QueryParameters.Orderby = parameters.OrderBy;
                }
            });
        }
    }
}
