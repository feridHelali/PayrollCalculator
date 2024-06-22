import { Repository } from "typeorm";
import AppDataSource from "../../main/typeorm.config";
import { SalaryTable } from "../db/entities/SalaryTable";

export class SalaryTableService {
    private repository: Repository<SalaryTable>;

    constructor() {
        this.repository = AppDataSource.getRepository(SalaryTable);
    }

    async findAll(): Promise<SalaryTable[]> {
        return this.repository.find({ relations: ["salaryTableCells", "agreement"] });
    }

    async findById(id: number): Promise<SalaryTable | undefined | null> {
        return this.repository.findOne({ where: { salaryTableId: id }, relations: ["salaryTableCells", "agreement"] });
    }

    async create(salaryTableData: Partial<SalaryTable>): Promise<SalaryTable> {
        return this.repository.save(salaryTableData);
    }

    async update(id: number, salaryTableData: Partial<SalaryTable>): Promise<SalaryTable | undefined | null> {
        await this.repository.update(id, salaryTableData);
        return this.findById(id);
    }

    async delete(id: string): Promise<void> {
        await this.repository.delete(id);
    }
}
