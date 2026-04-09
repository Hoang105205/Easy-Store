using CommunityToolkit.Mvvm.Messaging.Messages;
using System;

namespace UI.Messages;

public class CategorySelectedMessage : ValueChangedMessage<Guid?>
{
    public CategorySelectedMessage(Guid? categoryId) : base(categoryId) { }
}

public class CategoryDeletedMessage : ValueChangedMessage<Guid>
{
    public CategoryDeletedMessage(Guid categoryId) : base(categoryId) { }
}