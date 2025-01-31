using System.ComponentModel.DataAnnotations;
using MediatR;
using Moneo.TaskManagement.ResourceAccess.Entities;

namespace Moneo.TaskManagement.DomainEvents;

public abstract record DomainEvent(DateTime OccuredOn) : INotification;

public abstract record TaskDomainEvent(DateTime OccuredOn, MoneoTask Task) : DomainEvent(OccuredOn);
