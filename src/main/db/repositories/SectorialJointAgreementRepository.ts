import { Repository } from 'typeorm';
import  AppDataSource  from '../../typeorm.config';
import { SectorialJointAgreement } from '../entities/SectorialJointAgreement';

export class SectorialJointAgreementRepository {
  private repository: Repository<SectorialJointAgreement>;

  constructor() {
    this.repository = AppDataSource.getRepository(SectorialJointAgreement);
  }

  async findAll(): Promise<SectorialJointAgreement[]> {
    return this.repository.find();
  }

  async findById(id: number): Promise<SectorialJointAgreement | undefined> {
    return this.repository.findOne({ where: { sectorialJointAgreementId: id } }) as Promise<SectorialJointAgreement | undefined>;
  }

  async create(sectorialJointAgreementData: Partial<SectorialJointAgreement>): Promise<SectorialJointAgreement> {
    return this.repository.save(sectorialJointAgreementData);
  }

  async update(id: number, conventionData: Partial<SectorialJointAgreement>): Promise<SectorialJointAgreement | undefined> {
    await this.repository.update(id, conventionData);
    return this.findById(id);
  }

  async delete(id: number): Promise<void> {
    await this.repository.delete(id);
  }
}
