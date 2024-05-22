export class PagedResult<T> {
    pageNumber!: number;
    pageSize!: number;
    totalRecords!: number;
    data!: T[];
}