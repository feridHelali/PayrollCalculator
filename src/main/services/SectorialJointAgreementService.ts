import { SectorialJointAgreementRepository } from '../db/repositories/SectorialJointAgreementRepository';
import { SectorialJointAgreement } from '../db/entities/SectorialJointAgreement';

export class SecrorialJointAgreementService {
  private repository: SectorialJointAgreementRepository;

  constructor() {
    this.repository = new SectorialJointAgreementRepository();
  }

  async getAllSectorialJointAgreements(): Promise<SectorialJointAgreement[]> {
    return this.repository.findAll();
  }

  async getSectorialJointAgreementById(id: number): Promise<SectorialJointAgreement | undefined> {
    return this.repository.findById(id);
  }

  async createSectorialJointAgreement(sectorialJointAgreementData: Partial<SectorialJointAgreement>): Promise<SectorialJointAgreement> {
    return this.repository.create(sectorialJointAgreementData);
  }

  async updateSectorialJointAgreement(id: number, sectorialJointAgreementData: Partial<SectorialJointAgreement>): Promise<SectorialJointAgreement | undefined> {
    return this.repository.update(id, sectorialJointAgreementData);
  }

  async deleteSectorialJointAgreement(id: number): Promise<void> {
    return this.repository.delete(id);
  }
}
