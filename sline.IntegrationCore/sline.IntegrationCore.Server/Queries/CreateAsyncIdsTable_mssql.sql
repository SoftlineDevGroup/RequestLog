if OBJECT_ID('IntCore_Async_Ids') is null
CREATE TABLE IntCore_Async_Ids
(
	Guid varchar(50),
	RequestId varchar(max)
);