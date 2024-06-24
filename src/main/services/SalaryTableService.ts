import { FindOptionsWhere, Repository } from "typeorm";
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

    async getSalaryTablesByAgreementId(agreementId: number): Promise<SalaryTable[]> {
        const where: FindOptionsWhere<SalaryTable> = { agreement: { sectorialJointAgreementId: agreementId } };
        return await this.repository.find({ where });
    }
    async create(salaryTableData: Partial<SalaryTable>): Promise<SalaryTable> {
            return await this.repository.save({...salaryTableData,agreement:{sectorialJointAgreementId:salaryTableData.agreementId}});
         
    }

    async update(id: number, salaryTableData: Partial<SalaryTable>): Promise<SalaryTable | undefined | null> {
        await this.repository.update(id, salaryTableData);
        return await this.findById(id);
    }

    async delete(id: string): Promise<void> {
        console.log(`%cAttempting to delete SalaryTable with id: ${id}`,"background: red; color: white;font-weight: bold");
        const result=await this.repository.delete(id);
        console.log(`Delete result:`, result);
    }
}
