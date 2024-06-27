import { FindOptionsWhere, Repository } from "typeorm";
import AppDataSource from "../../main/typeorm.config";
import { SalaryTable } from "../db/entities/SalaryTable";
import { SalaryTableCell } from "../db/entities/SalaryTableCell";

export class SalaryTableService {
    private repository: Repository<SalaryTable>;
    private cellRepository: Repository<SalaryTableCell>;

    constructor() {
        this.repository = AppDataSource.getRepository(SalaryTable);
        this.cellRepository = AppDataSource.getRepository(SalaryTableCell);
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
        const newSalaryTable = await this.repository.save({
            ...salaryTableData,
            agreement: { sectorialJointAgreementId: salaryTableData.agreementId }
        });

        // Fetch the fully populated SalaryTable including its relations
        return this.repository.findOne({
            where: { salaryTableId: newSalaryTable.salaryTableId },
            relations: ["salaryTableCells", "agreement"],
        }) as Promise<SalaryTable>;
    }

    async update(id: number, salaryTableData: Partial<SalaryTable>): Promise<SalaryTable | undefined | null> {
        // Start a transaction to ensure data consistency
        return await AppDataSource.transaction(async transactionalEntityManager => {
            // Update the SalaryTable entity
            await transactionalEntityManager.update(SalaryTable, id, {
                agreementId: salaryTableData.agreementId,
                numeroTable: salaryTableData.numeroTable,
                type: salaryTableData.type,
                consernedEmployee: salaryTableData.consernedEmployee,
                beginningDateOfApplication: salaryTableData.beginningDateOfApplication,
                endDateOfApplication: salaryTableData.endDateOfApplication,
                degrees: salaryTableData.degrees,
                categories: salaryTableData.categories
            });

            console.log(salaryTableData)
            // Update the related SalaryTableCell entities
            if (salaryTableData.salaryTableCells) {
                // Delete existing SalaryTableCell entities
                await transactionalEntityManager.delete(SalaryTableCell, { salaryTable: { salaryTableId: id } });

                // Insert new SalaryTableCell entities
                for (const cellData of salaryTableData.salaryTableCells) {
                    const newCell = this.cellRepository.create({
                        ...cellData,
                        salaryTable: { salaryTableId: id }
                    });
                    await transactionalEntityManager.save(SalaryTableCell, newCell);
                }
            }


            // Fetch and return the updated SalaryTable entity with its relations
            return transactionalEntityManager.findOne(SalaryTable, {
                where: { salaryTableId: id },
                relations: ["salaryTableCells", "agreement"],
            });
        });
    }

    async delete(id: number): Promise<void> {
        console.log(`%cAttempting to delete SalaryTable with id: ${id}`, "background: red; color: white; font-weight: bold");
        const result = await this.repository.delete(id);
        console.log(`Delete result:`, result);
    }
}
