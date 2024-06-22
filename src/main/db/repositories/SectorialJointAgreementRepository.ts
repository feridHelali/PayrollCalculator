import { Repository } from 'typeorm';
import AppDataSource from '../../typeorm.config';
import { SectorialJointAgreement } from '../entities/SectorialJointAgreement';


export class SectorialJointAgreementRepository {
  private repository: Repository<SectorialJointAgreement>;

  constructor() {
    this.repository = AppDataSource.getRepository(SectorialJointAgreement);
  }

  async findAll(): Promise<SectorialJointAgreement[]> {
    return this.repository.find({
      order: {
        sectorialJointAgreementId: 'ASC',
      },
      relations: {
        salaryTables: true,
      }
    });
  }

  async findById(id: number): Promise<SectorialJointAgreement | undefined> {
    return this.repository.findOne({ where: { sectorialJointAgreementId: id }, relations: { salaryTables: true } }) as Promise<SectorialJointAgreement | undefined>;
  }

  async create(sectorialJointAgreementData: Partial<SectorialJointAgreement>): Promise<SectorialJointAgreement> {
    return this.repository.save(sectorialJointAgreementData);
  }

  async update(id: number, agreementData: Partial<SectorialJointAgreement>): Promise<SectorialJointAgreement | undefined> {
    const { salaryTables, ...updateData } = agreementData;
    await this.repository.update(id, updateData);
    return this.findById(id);
  }

  async delete(id: number): Promise<void> {
    await this.repository.delete(id);
  }
}
