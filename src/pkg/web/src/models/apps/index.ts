export interface IBuildJob {
    id: string;
    name: string;
    status: string;
    createdAt: string;
    finishedAt: string;
}

export interface ILogEntry {
    message: string;
    timestamp: string;
    level: string;
}