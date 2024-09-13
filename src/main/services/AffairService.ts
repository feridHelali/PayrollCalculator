// src\main\services\AffairService.ts
import { Repository } from 'typeorm';
import AppDataSource from '../../main/typeorm.config';
import { Affair } from '../db/entities/Affair';

export class AffairService {
  private repository: Repository<Affair>;

  constructor() {
    this.repository = AppDataSource.getRepository(Affair);
  }

  // Get all Affairs with optional ordering and relations to ActualMonthSalary
  async getAllAffairs(): Promise<Affair[]> {
    return this.repository.find({
      order: {
        affairId: 'ASC',  // Adjust the ordering field as needed
      },
      relations: {
        actualMonthSalaries: true,  // Load related ActualMonthSalary entities
      },
    });
  }

  // Get a single Affair by its ID, including relations to ActualMonthSalary
  async getAffairById(id: number): Promise<Affair | undefined | null> {
    return this.repository.findOne({ where: { affairId: id }, relations: { actualMonthSalaries: true } });
  }

  // Create a new Affair
  async createAffair(affairData: Partial<Affair>): Promise<Affair> {
    const newAffair=await this.repository.save( {
      ...affairData,
      agreementId:affairData.agreement });
    return  this.repository.findOne({
      where:{affairId:newAffair.affairId},
      relations:["sectorialJointAgreement"]
    }) as Promise<Affair>
  }

  // Update an existing Affair by its ID
  async updateAffair(id: number, affairData: Partial<Affair>): Promise<Affair | undefined | null> {
    const { actualMonthSalaries, ...updateData } = affairData;  // Separate related entities from other data
    await this.repository.update(id, updateData);
    return this.getAffairById(id);  // Return the updated Affair with relations
  }

  // Delete an Affair by its ID
  async deleteAffair(id: number): Promise<void> {
    await this.repository.delete(id);
  }
}
