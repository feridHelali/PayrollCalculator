import { Repository } from 'typeorm';
import AppDataSource from '../../main/typeorm.config';
import { SectorialJointAgreement } from '../db/entities/SectorialJointAgreement';

export class SectorialJointAgreementService {
  private repository: Repository<SectorialJointAgreement>;

  constructor() {
    this.repository = AppDataSource.getRepository(SectorialJointAgreement);
  }

  async getAllSectorialJointAgreements(): Promise<SectorialJointAgreement[]> {
    return this.repository.find({
      order: {
        sectorialJointAgreementId: 'ASC',
      },
      relations: {
        salaryTables: true,
      }
    });
  }

  async getSectorialJointAgreementById(id: number): Promise<SectorialJointAgreement | undefined> {
    return this.repository.findOne({ where: { sectorialJointAgreementId: id }, relations: { salaryTables: true } }) as Promise<SectorialJointAgreement | undefined>;
  }

  async createSectorialJointAgreement(sectorialJointAgreementData: Partial<SectorialJointAgreement>): Promise<SectorialJointAgreement> {
    return this.repository.save(sectorialJointAgreementData);
  }

  async updateSectorialJointAgreement(id: number, agreementData: Partial<SectorialJointAgreement>): Promise<SectorialJointAgreement | undefined> {
    const { salaryTables, ...updateData } = agreementData;
    await this.repository.update(id, updateData);
    return this.getSectorialJointAgreementById(id);
  }

  async deleteSectorialJointAgreement(id: number): Promise<void> {
    await this.repository.delete(id);
  }
}
