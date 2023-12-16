namespace IntegrationTests;

public class SchemaProviderFixture : DatabaseFixture
{
	public SchemaProviderFixture()
	{
		Connection.Execute("""
			DROP TABLE IF EXISTS fk_test;
			DROP TABLE IF EXISTS pk_test;

			CREATE TABLE pk_test
			(
				a INT NOT NULL,
				b INT NOT NULL,
				c INT NOT NULL,
				d INT NOT NULL,
				e INT NOT NULL,
				CONSTRAINT pk_test_pk PRIMARY KEY (a, b),
				CONSTRAINT pk_test_uq UNIQUE INDEX (c, d, e),
				INDEX pk_test_ix (c, d)
			);

			CREATE TABLE fk_test
			(
				g INT NOT NULL PRIMARY KEY AUTO_INCREMENT,
				h INT NOT NULL,
				i INT NOT NULL,
				CONSTRAINT fk_test_fk FOREIGN KEY (h, i) REFERENCES pk_test(a, b)
			);
			""");
	}
}
