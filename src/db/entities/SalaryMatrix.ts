import { Entity, PrimaryGeneratedColumn, Column, ManyToOne } from 'typeorm';
import { Category } from './Category';
import { Echelon } from './Echelon';

@Entity()
export class SalaryMatrix {
  @PrimaryGeneratedColumn()
  id!: number;

  @ManyToOne(() => Category, category => category.salaryMatrices)
  category: Category|undefined;

  @ManyToOne(() => Echelon, echelon => echelon.salaryMatrices)
  echelon: Echelon|undefined;

  @Column()
  salary: number = 0;
}