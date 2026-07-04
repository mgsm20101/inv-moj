using MediatR;

namespace WIMS.Application.Common.Messaging;

/// <summary>واجهة تعليم (Marker) للأوامر المُغيِّرة للحالة — تُدقَّق آلياً عبر AuditBehavior.</summary>
public interface IBaseCommand;

/// <summary>أمر يُرجع استجابة. الأوامر تُمثّل عمليات كتابة (Create/Update/Delete).</summary>
public interface ICommand<out TResponse> : IRequest<TResponse>, IBaseCommand;

/// <summary>استعلام يُرجع استجابة. الاستعلامات للقراءة فقط ولا تُدقَّق.</summary>
public interface IQuery<out TResponse> : IRequest<TResponse>;
