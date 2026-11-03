USE payroll_calculator_db
;

SET FOREIGN_KEY_CHECKS=0;




CREATE TABLE actualMonthSalary
(
	actualMonthSalaryID Integer NOT NULL,
	salaryPeriod string,
	currentProfessionalCategory string,
	currentProfessionalDegree int,
	actualSegniority int,
	theSalary money,
	affairID Integer,
	PRIMARY KEY (actualMonthSalaryID)
) 
;


CREATE TABLE affair
(
	affairID Integer NOT NULL,
	claimant string,
	startDateOfWork date,
	endDateOfWork date,
	professionalCategoryAtBegining string,
	professionalDegreeAtBegining int,
	sectorial joint AgreementID ,
	PRIMARY KEY (affairID)
) 
;


CREATE TABLE agreementApplicationPeriods
(
	agrrementApplicationPeriodsID Integer NOT NULL,
	start_date_of_application date,
	end_date_of_application date,
	description text,
	theAgreement ,
	PRIMARY KEY (agrrementApplicationPeriodsID)
) 
;


CREATE TABLE SalaryTable
(
	salaryTableID Integer NOT NULL,
	type enumeration,
	consernedEmployee string,
	professionalCategory string,
	prefessionalSubCategory string,
	ProfessiionalDegree int,
	segniorityForDegree int,
	salary mony,
	agrrementApplicationPeriodsID Integer,
	PRIMARY KEY (salaryTableID)
) 
;


CREATE TABLE SectorialJointAgreement
(
	sectorialJointAgreementID  NOT NULL,
	agreement_name string,
	description text,
	PRIMARY KEY (sectorialJointAgreementID)
) 
;



SET FOREIGN_KEY_CHECKS=1;
