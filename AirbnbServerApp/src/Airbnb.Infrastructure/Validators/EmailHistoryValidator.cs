﻿using System.Data;
using Airbnb.Domain.Entities;
using Airbnb.Domain.Enums;
using FluentValidation;

namespace Airbnb.Infrastructure.Validators;

public class EmailHistoryValidator : AbstractValidator<EmailHistory>
{
    public EmailHistoryValidator()
    {
        RuleSet(
            EntityEvent.OnCreate.ToString(),
            () =>
            {
                RuleFor(history => history.TemplateId).NotEqual(Guid.Empty);

                RuleFor(history => history.SenderUserId).NotEqual(Guid.Empty);

                RuleFor(history => history.ReceiverUserId).NotEqual(Guid.Empty);

                RuleFor(history => history.Content).NotEmpty().MaximumLength(129_536);

                RuleFor(history => history.ErrorMessage).NotNull().NotEmpty().When(history => !history.IsSuccessful);

                RuleFor(history => history.SenderEmailAddress).NotEmpty().MaximumLength(64);

                RuleFor(history => history.ReceiverEmailAddress).NotEmpty().MaximumLength(64);
            });
    }
}