using System;
using System.Collections.Generic;
using System.Text;

namespace Api.Utils;

public class ImportHelper
{
    public static long CalculateNewMacPrice(
        long currentStock,
        long currentMacPrice,
        int quantityAdded,
        long actualImportPrice)
    {
        long oldTotalValue = currentStock * currentMacPrice;
        long newTotalValue = quantityAdded * actualImportPrice;

        long newStockQuantity = currentStock + quantityAdded;

        long newMacPrice = (oldTotalValue + newTotalValue) / newStockQuantity;

        return newMacPrice;
    }
}
